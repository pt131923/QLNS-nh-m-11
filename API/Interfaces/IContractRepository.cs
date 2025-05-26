using API.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Interfaces
{
    public interface IContractRepository
    {
        Task<IEnumerable<Contract>> GetContractAsync();
        Task<Contract> GetContractByIdAsync(int id);
        Task<bool> SaveAllAsync();
        void Update(Contract contract);
        void Delete(Contract contract);
    }
}
