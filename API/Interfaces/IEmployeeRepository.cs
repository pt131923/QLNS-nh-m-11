using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<bool> SaveAllAsync();
        Task<IEnumerable<Employee>> GetEmployeesAsync();
        Task<Employee> GetEmployeeByIdAsync(int id);
        Task<IEnumerable<EmployeeDto>> GetEmployeeAsync();
        void Add(Employee employee);
        void Delete(Employee employee);
        void Update(Employee employee);
    }
}