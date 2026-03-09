using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using MongoDB.Driver;

namespace API.Data
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IMongoCollection<Employee> _employees;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IDashboardService _dashboardService;
        private readonly IMongoIdGenerator _idGenerator;

        private readonly List<Employee> _pendingInserts = new();
        private readonly List<Employee> _pendingUpdates = new();
        private readonly List<Employee> _pendingDeletes = new();

        public EmployeeRepository(
            IMongoDatabase database,
            AutoMapper.IMapper mapper,
            IDashboardService dashboardService,
            IMongoIdGenerator idGenerator)
        {
            _employees = database.GetCollection<Employee>("Employees");
            _mapper = mapper;
            _dashboardService = dashboardService;
            _idGenerator = idGenerator;
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync()
        {
            return await _employees.Find(_ => true).ToListAsync();
        }


        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
            return await _employees.Find(x => x.EmployeeId == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<EmployeeDto>> GetEmployeeAsync()
        {
            var employees = await _employees.Find(_ => true).ToListAsync();
            return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
        }

        public async Task<bool> SaveAllAsync()
        {
            var anyChanges = _pendingInserts.Count > 0 || _pendingUpdates.Count > 0 || _pendingDeletes.Count > 0;
            if (!anyChanges) return true;

            foreach (var d in _pendingDeletes)
            {
                await _employees.DeleteOneAsync(x => x.EmployeeId == d.EmployeeId);
            }

            foreach (var u in _pendingUpdates)
            {
                await _employees.ReplaceOneAsync(x => x.EmployeeId == u.EmployeeId, u, new ReplaceOptions { IsUpsert = false });
            }

            if (_pendingInserts.Count > 0)
            {
                foreach (var i in _pendingInserts)
                {
                    if (i.EmployeeId == 0)
                        i.EmployeeId = await _idGenerator.NextAsync("Employees");
                    if (i.CreatedAt == default)
                        i.CreatedAt = DateTime.UtcNow;
                }
                await _employees.InsertManyAsync(_pendingInserts);
            }

            _pendingDeletes.Clear();
            _pendingUpdates.Clear();
            _pendingInserts.Clear();

            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }


        public void Add(Employee employee)
        {
            _pendingInserts.Add(employee);
        }

        public void Delete(Employee employee)
        {
            _pendingDeletes.Add(employee);
        }

        public void Update(Employee employee)
        {
            _pendingUpdates.Add(employee);
        }
    }
}