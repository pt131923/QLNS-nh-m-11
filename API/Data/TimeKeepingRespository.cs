using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IMongoCollection<TimeKeeping> _timeKeeping = database.GetCollection<TimeKeeping>("TimeKeepings");
        private readonly IMongoCollection<Employee> _employees = database.GetCollection<Employee>("Employees");
        private readonly AutoMapper.IMapper _mapper = mapper;
        private readonly IDashboardService _dashboardService = dashboardService;
        private readonly IMongoIdGenerator _idGenerator = idGenerator;

        public async Task<TimeKeeping> CheckIn(string employeeId)
        {
            var id = ParseEmployeeId(employeeId);
            var now = DateTime.Now;
            var record = await FindTodayRecord(id, now);

            if (record != null)
            {
                if (HasTime(record.CheckIn) && !HasTime(record.CheckOut))
                    throw new InvalidOperationException("Bạn đã chấm công vào và chưa chấm công ra.");

                if (HasTime(record.CheckIn) && HasTime(record.CheckOut))
                    throw new InvalidOperationException("Hôm nay bạn đã hoàn thành chấm công.");

                record.CheckIn = now.TimeOfDay;
                record.CheckOut = TimeSpan.Zero;
                record.TotalHoursWorked = TimeSpan.Zero;
                record.TotalHours = 0;
                record.Status = "Đang làm việc";
                record.Note = "Chấm công vào";

                await _timeKeeping.ReplaceOneAsync(
                    x => x.TimeKeepingId == record.TimeKeepingId,
                    record);

                return record;
            }

            record = new TimeKeeping
            {
                TimeKeepingId = await _idGenerator.NextAsync("TimeKeepings"),
                EmployeeId = id,
                Date = now.Date,
                CheckIn = now.TimeOfDay,
                CheckOut = TimeSpan.Zero,
                TotalHoursWorked = TimeSpan.Zero,
                TotalHours = 0,
                Status = "Đang làm việc",
                Note = "Chấm công vào"
            };

            var employee = await _employees
                .Find(x => x.EmployeeId == id)
                .FirstOrDefaultAsync();

            if (employee != null)
            {
                record.DepartmentId = employee.DepartmentId;
                record.Employee = null;
            }

            await _timeKeeping.InsertOneAsync(record);
            return record;
        }

        public async Task<TimeKeeping> CheckOut(string employeeId)
        {
            var id = ParseEmployeeId(employeeId);
            var now = DateTime.Now;
            var record = await FindTodayRecord(id, now);

            if (record == null || !HasTime(record.CheckIn))
                throw new InvalidOperationException("Bạn chưa chấm công vào hôm nay.");

            if (HasTime(record.CheckOut))
                throw new InvalidOperationException("Hôm nay bạn đã chấm công ra.");

            var checkOut = now.TimeOfDay;
            if (checkOut < record.CheckIn)
                checkOut = record.CheckIn;

            record.CheckOut = checkOut;
            record.TotalHoursWorked = record.CheckOut - record.CheckIn;
            record.TotalHours = (decimal)record.TotalHoursWorked.TotalHours;
            record.Status = "Đã chấm công";
            record.Note = "Chấm công ra";

            await _timeKeeping.ReplaceOneAsync(
                x => x.TimeKeepingId == record.TimeKeepingId,
                record);

            return record;
        }

        public async Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingWithEmployees()
        {
            var timeKeeping = await _timeKeeping
                .Find(x => x.CheckIn != TimeSpan.Zero && x.CheckOut != TimeSpan.Zero)
                .ToListAsync();

            return MapWithEmployees(timeKeeping);
        }

        public async Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingWithEmployeesByDate(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);
            var timeKeeping = await _timeKeeping
                .Find(x => x.Date >= start && x.Date < end)
                .ToListAsync();

            return MapWithEmployees(timeKeeping);
        }

        public async Task<List<TimeKeepingWithEmployeeDto>> GetTimekeepingHistory()
        {
            var timeKeeping = await _timeKeeping
                .Find(x => x.CheckIn != TimeSpan.Zero && x.CheckOut != TimeSpan.Zero)
                .ToListAsync();

            return MapWithEmployees(timeKeeping);
        }

        public async Task<List<TimeKeepingDto>> GetTimeEntriesForUser(string employeeId)
        {
            var id = ParseEmployeeId(employeeId);
            var timeKeeping = await _timeKeeping
                .Find(x => x.EmployeeId == id)
                .SortByDescending(x => x.Date)
                .ToListAsync();

            return timeKeeping.Select(ToDto).ToList();
        }

        private async Task<TimeKeeping> FindTodayRecord(int employeeId, DateTime now)
        {
            var start = now.Date;
            var end = start.AddDays(1);

            return await _timeKeeping
                .Find(x => x.EmployeeId == employeeId && x.Date >= start && x.Date < end)
                .FirstOrDefaultAsync();
        }

        private List<TimeKeepingWithEmployeeDto> MapWithEmployees(List<TimeKeeping> timeKeeping)
        {
            if (timeKeeping == null || timeKeeping.Count == 0)
                return new List<TimeKeepingWithEmployeeDto>();

            return _mapper.Map<List<TimeKeepingWithEmployeeDto>>(timeKeeping)
                ?? new List<TimeKeepingWithEmployeeDto>();
        }

        private static TimeKeepingDto ToDto(TimeKeeping record)
        {
            return new TimeKeepingDto
            {
                TimeKeepingId = record.TimeKeepingId,
                EmployeeId = record.EmployeeId,
                Date = record.Date,
                CheckInTime = record.CheckIn,
                CheckOutTime = record.CheckOut,
                TotalHoursWorked = record.TotalHoursWorked,
                Status = record.Status,
                Note = record.Note,
                DepartmentId = record.DepartmentId
            };
        }

        private static int ParseEmployeeId(string employeeId)
        {
            if (!int.TryParse(employeeId, out var id) || id <= 0)
                throw new InvalidOperationException("Tài khoản chưa liên kết hồ sơ nhân viên.");

            return id;
        }

        private static bool HasTime(TimeSpan value)
        {
            return value > TimeSpan.Zero;
        }
    }
}
