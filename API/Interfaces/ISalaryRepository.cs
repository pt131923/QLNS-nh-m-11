using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface ISalaryRepository
    {
        Task<IEnumerable<Salary>> GetSalariesAsync();
        Task<IEnumerable<SalaryDto>> GetSalaryDtosAsync();
        Task<Salary> GetSalaryByIdAsync(int SalaryId);
        void Add(Salary salary);
        void Delete(Salary salary);
        void Update(Salary salary);
        Task<bool> SaveAllAsync();
    }
}
