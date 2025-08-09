using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class TimeKeepingRepository( DataContext _context, AutoMapper.IMapper _mapper) : ITimeKeepingRepository
    {
        public async Task<TimeKeeping> CreateTimeEntry(TimeKeepingDto timeKeepingDto)
        {
            var timeKeeping = _mapper.Map<TimeKeeping>(timeKeepingDto);
            await _context.TimeKeeping.AddAsync(timeKeeping);
            return timeKeeping;
        }

        public async Task<List<TimeKeepingDto>> GetTimeEntriesForUser(string employeeId)
        {
            var timeEntries = await _context.TimeKeeping
                .Where(x => x.EmployeeId == int.Parse(employeeId))
                .ToListAsync();

            return _mapper.Map<List<TimeKeepingDto>>(timeEntries);
        }

        public async Task<TimeKeepingWithEmployeeDto> GetTimeEntryById(int timeKeepingId)
        {
            var timeEntry = await _context.TimeKeeping
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.TimeKeepingId == timeKeepingId);

            return _mapper.Map<TimeKeepingWithEmployeeDto>(timeEntry);
        }

        public async Task<TimeKeeping> UpdateTimeEntry(TimeKeepingDto timeKeepingDto)
        {
            var timeKeeping = await _context.TimeKeeping
               .FirstOrDefaultAsync(x => x.TimeKeepingId == timeKeepingDto.TimeKeepingId);
            _mapper.Map(timeKeepingDto, timeKeeping);
            await _context.SaveChangesAsync();
            return timeKeeping;
        }

        public async Task<TimeKeepingDto> UpdateTimeEntry(int timeKeepingId, TimeKeepingUpdateDto timeKeepingUpdateDto)
        {
            var timeKeeping = await _context.TimeKeeping
                .FirstOrDefaultAsync(x => x.TimeKeepingId == timeKeepingId);

            _mapper.Map(timeKeepingUpdateDto, timeKeeping);
            _context.TimeKeeping.Update(timeKeeping);
            return await Task.FromResult(_mapper.Map<TimeKeepingDto>(timeKeeping));
        }
    }
}