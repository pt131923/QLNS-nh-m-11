using System;
using System.Threading.Tasks;
using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using API.Entities;
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
        private readonly IMongoDatabase _db;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
        private const string DASHBOARD_CACHE_KEY = "DashboardSummary";

        // Lưu trữ giá trị trước đó để so sánh
        private DashboardSummary _previousSummary;

        public DashboardService(
            IMongoDatabase db,
            IHubContext<DashboardHub> hubContext,
            IMemoryCache cache)
        {
            _db = db;
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
            var employees = _db.GetCollection<Employee>("Employees");
            var departments = _db.GetCollection<AppDepartment>("Departments");
            var contracts = _db.GetCollection<Contract>("Contracts");
            var contacts = _db.GetCollection<Contact>("Contacts");
            var salaries = _db.GetCollection<Salary>("Salaries");
            var timeKeeping = _db.GetCollection<TimeKeeping>("TimeKeeping");
            var trainings = _db.GetCollection<Training>("Trainings");
            var benefits = _db.GetCollection<Benefits>("Benefits");
            var leaves = _db.GetCollection<Leave>("Leaves");
            var recuiments = _db.GetCollection<Recuiment>("Recuiments");

            // Query song song
            var totalEmployeesTask = employees.CountDocumentsAsync(_ => true);
            var totalDepartmentsTask = departments.CountDocumentsAsync(_ => true);
            var totalContractsTask = contracts.CountDocumentsAsync(_ => true);
            var totalContactsTask = contacts.CountDocumentsAsync(_ => true);
            var totalTrainingCountTask = trainings.CountDocumentsAsync(_ => true);
            var totalRecruitmentCountTask = recuiments.CountDocumentsAsync(_ => true);
            var totalLeavesTask = leaves.CountDocumentsAsync(_ => true);

            var contactsProcessedTask = contacts.CountDocumentsAsync(c => c.Status == "Processed");
            var contactsPendingTask = contacts.CountDocumentsAsync(c => c.Status == "Pending");

            // Sum in-memory (đơn giản, đủ cho app nội bộ)
            var salariesListTask = salaries.Find(_ => true).Project(s => s.TotalSalary).ToListAsync();
            var timeKeepingListTask = timeKeeping.Find(_ => true).Project(t => t.TotalHours).ToListAsync();
            var benefitsListTask = benefits.Find(_ => true).Project(b => b.Cost).ToListAsync();

            await Task.WhenAll(
                totalEmployeesTask, totalDepartmentsTask, totalContractsTask, totalContactsTask,
                totalTrainingCountTask, totalRecruitmentCountTask, totalLeavesTask,
                contactsProcessedTask, contactsPendingTask,
                salariesListTask, timeKeepingListTask, benefitsListTask
            );

            var totalEmployees = (int)totalEmployeesTask.Result;
            var totalDepartments = (int)totalDepartmentsTask.Result;
            var totalContracts = (int)totalContractsTask.Result;
            var totalContacts = (int)totalContactsTask.Result;
            var totalTrainingCount = (int)totalTrainingCountTask.Result;
            var totalRecruitmentCount = (int)totalRecruitmentCountTask.Result;
            var totalLeavesCount = (int)totalLeavesTask.Result;

            var contactsProcessed = (int)contactsProcessedTask.Result;
            var contactsPending = (int)contactsPendingTask.Result;

            var totalSalaries = salariesListTask.Result.Sum();
            var totalWorkingHours = timeKeepingListTask.Result.Sum();
            var totalBenefitCost = benefitsListTask.Result.Sum();

            return new DashboardSummary(
                TotalEmployees: totalEmployees,
                TotalDepartments: totalDepartments,
                TotalContracts: totalContracts,
                TotalSalaries: totalSalaries,
                TotalTimekeeping: totalWorkingHours,
                TotalRecruitments: totalRecruitmentCount,
                TotalBenefits: totalBenefitCost,
                TotalTrainings: totalTrainingCount,
                TotalLeaves: totalLeavesCount,

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
