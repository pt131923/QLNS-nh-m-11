using API.Data;
using API.Entities;
using API.Interfaces;
using API.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace API.Data
{
    public class ContractRepository : IContractRepository
    {
        private readonly IMongoCollection<Contract> _contracts;
        private readonly IDashboardService _dashboardService;
        private readonly IMongoIdGenerator _idGenerator;

        private readonly List<Contract> _pendingInserts = new();
        private readonly List<Contract> _pendingUpdates = new();
        private readonly List<Contract> _pendingDeletes = new();

        public ContractRepository(
            IMongoDatabase database,
            IDashboardService dashboardService,
            IMongoIdGenerator idGenerator)
        {
            _contracts = database.GetCollection<Contract>("Contracts");
            _dashboardService = dashboardService;
            _idGenerator = idGenerator;
        }

        public async Task<IEnumerable<Contract>> GetContractAsync()
        {
            return await _contracts.Find(_ => true).ToListAsync();
        }

        public async Task<Contract> GetContractByIdAsync(int id)
        {
            return await _contracts.Find(c => c.ContractId == id).FirstOrDefaultAsync();
        }

        public void Add(Contract contract)
        {
            _pendingInserts.Add(contract);
        }

        public void Update(Contract contract)
        {
            _pendingUpdates.Add(contract);
        }

        public void Delete(Contract contract)
        {
            _pendingDeletes.Add(contract);
        }

        public async Task<bool> SaveAllAsync()
        {
            var anyChanges = _pendingInserts.Count > 0 || _pendingUpdates.Count > 0 || _pendingDeletes.Count > 0;
            if (!anyChanges) return true;

            foreach (var d in _pendingDeletes)
            {
                await _contracts.DeleteOneAsync(x => x.ContractId == d.ContractId);
            }

            foreach (var u in _pendingUpdates)
            {
                await _contracts.ReplaceOneAsync(x => x.ContractId == u.ContractId, u, new ReplaceOptions { IsUpsert = false });
            }

            if (_pendingInserts.Count > 0)
            {
                foreach (var i in _pendingInserts)
                {
                    if (i.ContractId == 0)
                        i.ContractId = await _idGenerator.NextAsync("Contracts");
                    if (i.CreateAt == default)
                        i.CreateAt = DateTime.UtcNow;
                    if (i.UpdateAt == default)
                        i.UpdateAt = DateTime.UtcNow;
                }
                await _contracts.InsertManyAsync(_pendingInserts);
            }

            _pendingDeletes.Clear();
            _pendingUpdates.Clear();
            _pendingInserts.Clear();

            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }
    }
}
