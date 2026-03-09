using System.Collections.Generic;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using API.Services;
using MongoDB.Driver;

namespace API.Data
{
    public class TrainingRepository : ITrainingRepository
    {
        private readonly IMongoCollection<Training> _trainings;
        private readonly IDashboardService _dashboardService;
        private readonly IMongoIdGenerator _idGenerator;

        private readonly List<Training> _pendingInserts = new();
        private readonly List<Training> _pendingUpdates = new();
        private readonly List<Training> _pendingDeletes = new();

        public TrainingRepository(
            IMongoDatabase database,
            IDashboardService dashboardService,
            IMongoIdGenerator idGenerator)
        {
            _trainings = database.GetCollection<Training>("Trainings");
            _dashboardService = dashboardService;
            _idGenerator = idGenerator;
        }

        // Lấy danh sách Training
        public async Task<IEnumerable<Training>> GetTrainingsAsync()
        {
            return await _trainings.Find(_ => true).ToListAsync();
        }

        // Lấy Training theo ID
        public async Task<Training> GetTrainingByIdAsync(int id)
        {
            return await _trainings.Find(x => x.TrainingId == id).FirstOrDefaultAsync();
        }

        // Thêm
        public void Add(Training training)
        {
            _pendingInserts.Add(training);
        }

        // Cập nhật
        public void Update(Training training)
        {
            _pendingUpdates.Add(training);
        }

        // Xóa
        public void Delete(Training training)
        {
            _pendingDeletes.Add(training);
        }

        // Lưu thay đổi
        public async Task<bool> SaveAllAsync()
        {
            var anyChanges = _pendingInserts.Count > 0 || _pendingUpdates.Count > 0 || _pendingDeletes.Count > 0;
            if (!anyChanges) return true;

            foreach (var d in _pendingDeletes)
                await _trainings.DeleteOneAsync(x => x.TrainingId == d.TrainingId);

            foreach (var u in _pendingUpdates)
                await _trainings.ReplaceOneAsync(x => x.TrainingId == u.TrainingId, u, new ReplaceOptions { IsUpsert = false });

            if (_pendingInserts.Count > 0)
            {
                foreach (var i in _pendingInserts)
                {
                    if (i.TrainingId == 0)
                        i.TrainingId = await _idGenerator.NextAsync("Trainings");
                }
                await _trainings.InsertManyAsync(_pendingInserts);
            }

            _pendingDeletes.Clear();
            _pendingUpdates.Clear();
            _pendingInserts.Clear();

            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }
    }
}