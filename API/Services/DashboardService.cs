using System;
using System.Threading.Tasks;
using API.Data;
using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace API.Services
{
    // Interface cho DashboardService để dễ dàng testing và dependency injection
    public interface IDashboardService
    {
        Task<DashboardSummary> GetDashboardSummaryAsync(bool forceRefresh = false);
        Task TriggerRealtimeUpdateAsync();
        Task NotifyDataChangedAsync();
        Task NotifyDataChangedWithCheckAsync();
    }

    public record DashboardSummary(
        int TotalEmployees,
        int TotalDepartments,
        int TotalContracts,
        decimal TotalSalaries,
        decimal TotalTimekeeping,
        int TotalRecruitments,
        decimal TotalBenefits,
        int TotalTrainings,
        int TotalLeaves,

        int TotalContacts,
        int ContactsProcessed,
        int ContactsPending,

        int Settings,
        int Help,
        DateTime LastUpdated
    );

    public class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
        private const string DASHBOARD_CACHE_KEY = "DashboardSummary";

        // Lưu trữ giá trị trước đó để so sánh
        private DashboardSummary _previousSummary;

        public DashboardService(
            IDbContextFactory<DataContext> dbContextFactory,
            IHubContext<DashboardHub> hubContext,
            IMemoryCache cache)
        {
            _dbContextFactory = dbContextFactory;
            _hubContext = hubContext;
            _cache = cache;
        }

        public async Task<DashboardSummary> GetDashboardSummaryAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cache.TryGetValue(DASHBOARD_CACHE_KEY, out DashboardSummary cachedSummary))
            {
                return cachedSummary;
            }

            var summary = await CalculateDashboardSummaryAsync();

            // Cache kết quả
            _cache.Set(DASHBOARD_CACHE_KEY, summary, _cacheDuration);

            return summary;
        }

        private async Task<DashboardSummary> CalculateDashboardSummaryAsync()
        {
            // Hàm utility để tạo context mới cho mỗi truy vấn
            async Task<T> ExecuteQuery<T>(Func<DataContext, Task<T>> queryFunc)
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await queryFunc(context);
            }

            // Thực thi các truy vấn song song để tối ưu hiệu suất
            var totalEmployeesTask = ExecuteQuery(c => c.Employee.CountAsync());
            var totalDepartmentsTask = ExecuteQuery(c => c.Department.CountAsync());
            var totalContractsTask = ExecuteQuery(c => c.Contract.CountAsync());
            var totalContactsTask = ExecuteQuery(c => c.Contact.CountAsync());

            var totalSalariesTask = ExecuteQuery(c => c.Salary.SumAsync(s => (decimal?)s.TotalSalary));
            var totalWorkingHoursTask = ExecuteQuery(c => c.TimeKeeping.SumAsync(t => (decimal?)t.TotalHours));
            var totalTrainingCountTask = ExecuteQuery(c => c.Training.CountAsync());
            var totalBenefitCostTask = ExecuteQuery(c => c.Benefits.SumAsync(b => (decimal?)b.Cost));

            var totalLeaveDaysTask = ExecuteQuery(c => c.LeaveRequest.SumAsync(l => (int?)l.Days));
            var totalRecruitmentCountTask = ExecuteQuery(c => c.Recuiment.CountAsync());

            var contactsProcessedTask = ExecuteQuery(c => c.Contact.CountAsync(contact => contact.Status == "Processed"));
            var contactsPendingTask = ExecuteQuery(c => c.Contact.CountAsync(contact => contact.Status == "Pending"));

            await Task.WhenAll(
                totalEmployeesTask, totalDepartmentsTask, totalContractsTask, totalContactsTask,
                totalSalariesTask, totalWorkingHoursTask, totalLeaveDaysTask,
                totalRecruitmentCountTask, totalTrainingCountTask, totalBenefitCostTask,
                contactsProcessedTask, contactsPendingTask
            );

            // Extract results and handle nulls
            var totalEmployees = totalEmployeesTask.Result;
            var totalDepartments = totalDepartmentsTask.Result;
            var totalContracts = totalContractsTask.Result;
            var totalContacts = totalContactsTask.Result;
            var contactsProcessed = contactsProcessedTask.Result;
            var contactsPending = contactsPendingTask.Result;

            var totalSalaries = totalSalariesTask.Result ?? 0m;
            var totalWorkingHours = totalWorkingHoursTask.Result ?? 0m;
            var totalTrainingCount = totalTrainingCountTask.Result;
            var totalBenefitCost = totalBenefitCostTask.Result ?? 0m;
            var totalLeaveDays = totalLeaveDaysTask.Result ?? 0;
            var totalRecruitmentCount = totalRecruitmentCountTask.Result;

            return new DashboardSummary(
                TotalEmployees: totalEmployees,
                TotalDepartments: totalDepartments,
                TotalContracts: totalContracts,
                TotalSalaries: totalSalaries,
                TotalTimekeeping: totalWorkingHours,
                TotalRecruitments: totalRecruitmentCount,
                TotalBenefits: totalBenefitCost,
                TotalTrainings: totalTrainingCount,
                TotalLeaves: totalLeaveDays,

                TotalContacts: totalContacts,
                ContactsProcessed: contactsProcessed,
                ContactsPending: contactsPending,

                Settings: 1,
                Help: 1,
                LastUpdated: DateTime.Now
            );
        }

        public async Task TriggerRealtimeUpdateAsync()
        {
            var summary = await GetDashboardSummaryAsync(forceRefresh: true);
            await _hubContext.Clients.All.SendAsync("ReceiveDashboardUpdate", summary);
        }

        public async Task NotifyDataChangedAsync()
        {
            // Xóa cache để lần sau sẽ tính toán lại
            _cache.Remove(DASHBOARD_CACHE_KEY);

            // Trigger cập nhật realtime
            await TriggerRealtimeUpdateAsync();
        }

        // Method để kiểm tra và trigger update chỉ khi số lượng thực sự thay đổi
        public async Task NotifyDataChangedWithCheckAsync()
        {
            var currentSummary = await GetDashboardSummaryAsync(forceRefresh: true);

            // So sánh với summary trước đó
            if (HasDataChanged(currentSummary, _previousSummary))
            {
                _previousSummary = currentSummary;
                await TriggerRealtimeUpdateAsync();
            }
        }

        private bool HasDataChanged(DashboardSummary current, DashboardSummary previous)
        {
            if (previous == null) return true; // Lần đầu tiên luôn trigger

            // So sánh các trường số lượng quan trọng
            return current.TotalEmployees != previous.TotalEmployees ||
                   current.TotalDepartments != previous.TotalDepartments ||
                   current.TotalContracts != previous.TotalContracts ||
                   current.TotalContacts != previous.TotalContacts ||
                   current.TotalRecruitments != previous.TotalRecruitments ||
                   current.TotalLeaves != previous.TotalLeaves ||
                   current.ContactsProcessed != previous.ContactsProcessed ||
                   current.ContactsPending != previous.ContactsPending;
        }
    }
}
