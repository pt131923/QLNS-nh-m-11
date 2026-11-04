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

        public async Task<TimeKeeping> CheckIn(string employeeId)
        {
            var timeKeeping = await _timeKeeping.Find(x => x.EmployeeId == int.Parse(employeeId)).FirstOrDefaultAsync();
            if (timeKeeping == null) return null;

            timeKeeping.CheckIn = TimeSpan.FromHours(DateTime.UtcNow.Hour);
            await _timeKeeping.ReplaceOneAsync(x => x.TimeKeepingId == timeKeeping.TimeKeepingId, timeKeeping);
            return timeKeeping;
        }

        public async Task<TimeKeeping> CheckOut(string employeeId)
        {
            var timeKeeping = await _timeKeeping.Find(x => x.EmployeeId == int.Parse(employeeId)).FirstOrDefaultAsync();
            if (timeKeeping == null) return null;

            timeKeeping.CheckOut = TimeSpan.FromHours(DateTime.UtcNow.Hour);
            await _timeKeeping.ReplaceOneAsync(x => x.TimeKeepingId == timeKeeping.TimeKeepingId, timeKeeping);
            return timeKeeping;
        }
        public async Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingWithEmployees()
        {
            var timeKeeping = await _timeKeeping.Find(x => x.CheckIn != TimeSpan.Zero && x.CheckOut != TimeSpan.Zero).ToListAsync();
            if (timeKeeping == null) return null;

            var employees = await _employees.Find(x => timeKeeping.Any(y => y.EmployeeId == x.EmployeeId)).ToListAsync();
            return _mapper.Map<List<TimeKeepingWithEmployeeDto>>(timeKeeping);
        }

		public async Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingWithEmployeesByDate(DateTime date)
        {
            var timeKeeping = await _timeKeeping.Find(x => x.Date == date).ToListAsync();
            if (timeKeeping == null) return null;

            var employees = await _employees.Find(x => timeKeeping.Any(y => y.EmployeeId == x.EmployeeId)).ToListAsync();
            return _mapper.Map<List<TimeKeepingWithEmployeeDto>>(timeKeeping);
        }

        public async Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingHistory()
        {
            var timeKeeping = await _timeKeeping.Find(x => x.CheckIn != TimeSpan.Zero && x.CheckOut != TimeSpan.Zero).ToListAsync();
            if (timeKeeping == null) return null;

            var employees = await _employees.Find(x => timeKeeping.Any(y => y.EmployeeId == x.EmployeeId)).ToListAsync();
            return _mapper.Map<List<TimeKeepingWithEmployeeDto>>(timeKeeping);
        }

		public async Task<List<TimeKeepingDto>> GetTimeEntriesForUser(string employeeId)
        {
            var timeKeeping = await _timeKeeping.Find(x => x.EmployeeId == int.Parse(employeeId)).ToListAsync();
            if (timeKeeping == null) return null;

            var employees = await _employees.Find(x => timeKeeping.Any(y => y.EmployeeId == x.EmployeeId)).ToListAsync();
            return _mapper.Map<List<TimeKeepingDto>>(timeKeeping);
        }
	}
}