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
        Task<List<TimeKeeping>> GetTimeEntriesForUser(string employeeId);
        Task<TimeKeeping> CreateTimeEntry(TimeKeepingDto timeKeepingDto);
        Task<TimeKeeping> UpdateTimeEntry(int timeKeepingId, TimeKeepingUpdateDto timeKeepingUpdateDto);
        Task<bool> DeleteTimeEntry(int timeKeepingId);
        Task<TimeKeepingWithEmployeeDto> GetTimeEntryById(int timeKeepingId);
    }
}