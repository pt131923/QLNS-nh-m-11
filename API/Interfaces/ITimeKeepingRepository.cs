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
        Task<TimeKeepingDto> UpdateTimeEntry(int timeKeepingId, TimeKeepingUpdateDto timeKeepingUpdateDto);
        Task<TimeKeepingWithEmployeeDto> GetTimeEntryById(int timeKeepingId);
        Task<TimeKeeping> UpdateTimeEntry(TimeKeepingDto timeKeepingDto);
        Task<TimeKeeping> CreateTimeEntry(TimeKeepingDto timeKeepingDto);

    }
}