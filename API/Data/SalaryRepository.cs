using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using MongoDB.Driver;

namespace API.Data
{
    public class SalaryRepository : ISalaryRepository
    {
        private readonly IMongoCollection<Salary> _salaries;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IDashboardService _dashboardService;
        private readonly IMongoIdGenerator _idGenerator;

        private readonly List<Salary> _pendingInserts = new();
        private readonly List<Salary> _pendingUpdates = new();
        private readonly List<Salary> _pendingDeletes = new();

        public SalaryRepository(
            IMongoDatabase database,
            AutoMapper.IMapper mapper,
            IDashboardService dashboardService,
            IMongoIdGenerator idGenerator)
        {
            _salaries = database.GetCollection<Salary>("Salaries");
            _mapper = mapper;
            _dashboardService = dashboardService;
            _idGenerator = idGenerator;
        }

        public async Task<IEnumerable<Salary>> GetSalariesAsync()
        {
            return await _salaries.Find(_ => true).ToListAsync();
        }

       public async Task<Salary> GetSalaryByIdAsync(int id)
       {
            return await _salaries.Find(x => x.SalaryId == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SalaryDto>> GetSalaryDtosAsync()
        {
            var salaries = await _salaries.Find(_ => true).ToListAsync();
            return _mapper.Map<IEnumerable<SalaryDto>>(salaries);
        }

        public async Task<bool> SaveAllAsync()
        {
            var anyChanges = _pendingInserts.Count > 0 || _pendingUpdates.Count > 0 || _pendingDeletes.Count > 0;
            if (!anyChanges) return true;

            foreach (var d in _pendingDeletes)
            {
                await _salaries.DeleteOneAsync(x => x.SalaryId == d.SalaryId);
            }

            foreach (var u in _pendingUpdates)
            {
                await _salaries.ReplaceOneAsync(x => x.SalaryId == u.SalaryId, u, new ReplaceOptions { IsUpsert = false });
            }

            if (_pendingInserts.Count > 0)
            {
                foreach (var i in _pendingInserts)
                {
                    if (i.SalaryId == 0)
                        i.SalaryId = await _idGenerator.NextAsync("Salaries");
                    if (i.Date == default)
                        i.Date = DateTime.UtcNow;
                }
                await _salaries.InsertManyAsync(_pendingInserts);
            }

            _pendingDeletes.Clear();
            _pendingUpdates.Clear();
            _pendingInserts.Clear();

            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }

        public void Add(Salary salary)
        {
            _pendingInserts.Add(salary);
        }

        public void Delete(Salary salary)
        {
            _pendingDeletes.Add(salary);
        }

        public void Update(Salary salary)
        {
            _pendingUpdates.Add(salary);
        }
    }
}
