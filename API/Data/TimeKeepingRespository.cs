using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using MongoDB.Driver;

namespace API.Data
{
    public class TimeKeepingRepository(
        IMongoDatabase database,
        AutoMapper.IMapper mapper,
        IDashboardService dashboardService,
        IMongoIdGenerator idGenerator) : ITimeKeepingRepository
    {
        private readonly IMongoCollection<TimeKeeping> _timeKeeping = database.GetCollection<TimeKeeping>("TimeKeeping");
        private readonly IMongoCollection<Employee> _employees = database.GetCollection<Employee>("Employees");
        private readonly AutoMapper.IMapper _mapper = mapper;
        private readonly IDashboardService _dashboardService = dashboardService;
        private readonly IMongoIdGenerator _idGenerator = idGenerator;

        public async Task<TimeKeeping> CreateTimeEntry(TimeKeepingDto timeKeepingDto)
        {
            var timeKeeping = _mapper.Map<TimeKeeping>(timeKeepingDto);
            if (timeKeeping.TimeKeepingId == 0)
                timeKeeping.TimeKeepingId = await _idGenerator.NextAsync("TimeKeeping");
            if (timeKeeping.Date == default)
                timeKeeping.Date = DateTime.UtcNow;

            await _timeKeeping.InsertOneAsync(timeKeeping);

            // Trigger cập nhật dashboard khi có thay đổi thời gian làm việc
            await _dashboardService.NotifyDataChangedWithCheckAsync();

            return timeKeeping;
        }

        public async Task<List<TimeKeepingDto>> GetTimeEntriesForUser(string employeeId)
        {
            var empId = int.Parse(employeeId);
            var timeEntries = await _timeKeeping.Find(x => x.EmployeeId == empId).ToListAsync();

            return _mapper.Map<List<TimeKeepingDto>>(timeEntries);
        }

        public async Task<TimeKeepingWithEmployeeDto> GetTimeEntryById(int timeKeepingId)
        {
            var timeEntry = await _timeKeeping.Find(x => x.TimeKeepingId == timeKeepingId).FirstOrDefaultAsync();
            if (timeEntry == null) return null;

            var employee = await _employees.Find(e => e.EmployeeId == timeEntry.EmployeeId).FirstOrDefaultAsync();
            timeEntry.Employee = employee;

            return _mapper.Map<TimeKeepingWithEmployeeDto>(timeEntry);
        }

        public async Task<TimeKeeping> UpdateTimeEntry(TimeKeepingDto timeKeepingDto)
        {
            var timeKeeping = await _timeKeeping.Find(x => x.TimeKeepingId == timeKeepingDto.TimeKeepingId).FirstOrDefaultAsync();
            if (timeKeeping == null) return null;

            _mapper.Map(timeKeepingDto, timeKeeping);
            await _timeKeeping.ReplaceOneAsync(x => x.TimeKeepingId == timeKeeping.TimeKeepingId, timeKeeping);

            // Trigger cập nhật dashboard khi có thay đổi thời gian làm việc
            await _dashboardService.NotifyDataChangedWithCheckAsync();

            return timeKeeping;
        }

        public async Task<TimeKeepingDto> UpdateTimeEntry(int timeKeepingId, TimeKeepingUpdateDto timeKeepingUpdateDto)
        {
            var timeKeeping = await _timeKeeping.Find(x => x.TimeKeepingId == timeKeepingId).FirstOrDefaultAsync();
            if (timeKeeping == null) return null;

            _mapper.Map(timeKeepingUpdateDto, timeKeeping);
            await _timeKeeping.ReplaceOneAsync(x => x.TimeKeepingId == timeKeeping.TimeKeepingId, timeKeeping);

            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return _mapper.Map<TimeKeepingDto>(timeKeeping);
        }
    }
}