using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;

namespace API.Interfaces
{
    public interface ILeaveRepository
    {
        Task<IEnumerable<Leave>> GetAllLeavesAsync();
        Task<Leave> GetLeaveByUserId(int userId);
        Task<IEnumerable<Leave>> GetLeavesAsync();
        Task<Leave> GetLeaveByIdAsync(int leaveId);
        Task<bool> SaveLeaveAsync(Leave leave);
        Task<bool> LeaveExistsAsync(int leaveId);
        Task<bool> AddLeave(Leave leave);
        Task<bool> SaveChanges();
    }
}