using System.Collections.Generic;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using API.Services;
using MongoDB.Driver;

namespace API.Data
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly IMongoCollection<Leave> _leaves;
        private readonly IDashboardService _dashboardService;
        private readonly IMongoIdGenerator _idGenerator;

        public LeaveRepository(
            IMongoDatabase database,
            IDashboardService dashboardService,
            IMongoIdGenerator idGenerator)
        {
            _leaves = database.GetCollection<Leave>("Leaves");
            _dashboardService = dashboardService;
            _idGenerator = idGenerator;
        }

        public async Task<IEnumerable<Leave>> GetAllLeavesAsync()
        {
            return await _leaves.Find(_ => true).ToListAsync();
        }

        public async Task<Leave> GetLeaveByUserId(int userId)
        {
            return await _leaves.Find(x => x.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Leave>> GetLeavesAsync()
        {
            return await _leaves.Find(_ => true).ToListAsync();
        }

        public async Task<Leave> GetLeaveByIdAsync(int leaveId)
        {
            return await _leaves.Find(x => x.LeaveId == leaveId).FirstOrDefaultAsync();
        }

        public async Task<bool> SaveLeaveAsync(Leave leave)
        {
            if (leave.LeaveId == 0)
                leave.LeaveId = await _idGenerator.NextAsync("Leaves");
            if (string.IsNullOrWhiteSpace(leave.Status))
                leave.Status = "Pending";

            await _leaves.InsertOneAsync(leave);
            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }

        public async Task<bool> LeaveExistsAsync(int leaveId)
        {
            var count = await _leaves.CountDocumentsAsync(x => x.LeaveId == leaveId);
            return count > 0;
        }

        public async Task<bool> AddLeave(Leave leave)
        {
            return await SaveLeaveAsync(leave);
        }

        public async Task<bool> SaveChanges()
        {
            // MongoDB ghi trực tiếp; chỉ trigger dashboard
            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }
    }
}