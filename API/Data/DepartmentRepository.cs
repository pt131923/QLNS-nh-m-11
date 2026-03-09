using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using MongoDB.Driver;

namespace API.Data
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly IMongoCollection<AppDepartment> _departments;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IDashboardService _dashboardService;
        private readonly IMongoIdGenerator _idGenerator;

        private readonly List<AppDepartment> _pendingInserts = new();
        private readonly List<AppDepartment> _pendingUpdates = new();
        private readonly List<AppDepartment> _pendingDeletes = new();

        public DepartmentRepository(
            IMongoDatabase database,
            AutoMapper.IMapper mapper,
            IDashboardService dashboardService,
            IMongoIdGenerator idGenerator)
        {
            _departments = database.GetCollection<AppDepartment>("Departments");
            _mapper = mapper;
            _dashboardService = dashboardService;
            _idGenerator = idGenerator;
        }

        public async Task<IEnumerable<AppDepartment>> GetDepartmentsAsync()
        {
            return await _departments.Find(_ => true).ToListAsync();
        }


        public async Task<AppDepartment> GetDepartmentByIdAsync(int id)
        {
            return await _departments.Find(x => x.DepartmentId == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<DepartmentDto>> GetDepartmentAsync()
        {
            var departments = await _departments.Find(_ => true).ToListAsync();
            return _mapper.Map<IEnumerable<DepartmentDto>>(departments);
        }

        public async Task<bool> SaveAllAsync()
        {
            var anyChanges = _pendingInserts.Count > 0 || _pendingUpdates.Count > 0 || _pendingDeletes.Count > 0;
            if (!anyChanges) return true;

            foreach (var d in _pendingDeletes)
            {
                await _departments.DeleteOneAsync(x => x.DepartmentId == d.DepartmentId);
            }

            foreach (var u in _pendingUpdates)
            {
                await _departments.ReplaceOneAsync(x => x.DepartmentId == u.DepartmentId, u, new ReplaceOptions { IsUpsert = false });
            }

            if (_pendingInserts.Count > 0)
            {
                foreach (var i in _pendingInserts)
                {
                    if (i.DepartmentId == 0)
                        i.DepartmentId = await _idGenerator.NextAsync("Departments");
                }
                await _departments.InsertManyAsync(_pendingInserts);
            }

            _pendingDeletes.Clear();
            _pendingUpdates.Clear();
            _pendingInserts.Clear();

            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }

        public void Add(AppDepartment depart)
        {
            _pendingInserts.Add(depart);
        }

        public void Delete(AppDepartment depart)
        {
            _pendingDeletes.Add(depart);
        }

        public void Update(AppDepartment depart)
        {
            _pendingUpdates.Add(depart);
        }

        public async Task<bool> DepartmentExists(string departmentName)
        {
            if (string.IsNullOrWhiteSpace(departmentName)) return false;
            var cleaned = departmentName.Trim();
            var filter = Builders<AppDepartment>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleaned)}$", "i"));
            var count = await _departments.CountDocumentsAsync(filter);
            return count > 0;
        }
    }
}