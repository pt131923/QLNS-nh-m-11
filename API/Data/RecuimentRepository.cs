using System.Collections.Generic;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using API.Services;
using MongoDB.Driver;

namespace API.Data
{
    public class RecuimentRepository : IRecuimentRepository
    {
        private readonly IMongoCollection<Recuiment> _recuiments;
        private readonly IDashboardService _dashboardService;
        private readonly IMongoIdGenerator _idGenerator;

        private readonly List<Recuiment> _pendingInserts = new();
        private readonly List<Recuiment> _pendingUpdates = new();
        private readonly List<Recuiment> _pendingDeletes = new();

        public RecuimentRepository(
            IMongoDatabase database,
            IDashboardService dashboardService,
            IMongoIdGenerator idGenerator)
        {
            _recuiments = database.GetCollection<Recuiment>("Recuiments");
            _dashboardService = dashboardService;
            _idGenerator = idGenerator;
        }

        // Lấy danh sách Recuiment
        public async Task<IEnumerable<Recuiment>> GetRecuimentsAsync()
        {
            return await _recuiments.Find(_ => true).ToListAsync();
        }

        // Lấy Recuiment theo ID
        public async Task<Recuiment> GetRecuimentByIdAsync(int id)
        {
            return await _recuiments.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        // Thêm
        public void Add(Recuiment recuiment)
        {
            _pendingInserts.Add(recuiment);
        }

        // Cập nhật
        public void Update(Recuiment recuiment)
        {
            _pendingUpdates.Add(recuiment);
        }

        // Xóa
        public void Delete(Recuiment recuiment)
        {
            _pendingDeletes.Add(recuiment);
        }

        // Lưu thay đổi
        public async Task<bool> SaveAllAsync()
        {
            var anyChanges = _pendingInserts.Count > 0 || _pendingUpdates.Count > 0 || _pendingDeletes.Count > 0;
            if (!anyChanges) return true;

            foreach (var d in _pendingDeletes)
                await _recuiments.DeleteOneAsync(x => x.Id == d.Id);

            foreach (var u in _pendingUpdates)
                await _recuiments.ReplaceOneAsync(x => x.Id == u.Id, u, new ReplaceOptions { IsUpsert = false });

            if (_pendingInserts.Count > 0)
            {
                foreach (var i in _pendingInserts)
                {
                    if (i.Id == 0)
                        i.Id = await _idGenerator.NextAsync("Recuiments");
                }
                await _recuiments.InsertManyAsync(_pendingInserts);
            }

            _pendingDeletes.Clear();
            _pendingUpdates.Clear();
            _pendingInserts.Clear();

            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }
    }
}
