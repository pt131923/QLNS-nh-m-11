using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface ITimeKeepingRepository
    {
        Task<List<TimeKeepingDto>> GetTimeEntriesForUser(string employeeId);
        Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingHistory();
        Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingWithEmployees();
        Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingWithEmployeesByDate(DateTime date);
        Task<TimeKeeping> CheckIn(string employeeId);
        Task<TimeKeeping> CheckOut(string employeeId);
    }
}