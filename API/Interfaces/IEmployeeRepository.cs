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
        void Update(int id, Employee employee);
        Task<EmployeeDto> AddEmployeeAsync(EmployeeDto dto);
        Task<bool> UpdateEmployeeAsync(int id, EmployeeUpdateDto dto);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<bool> GetWithDepartmentsAsync();
        Task<(int total, int imported, List<string> errors)> ImportEmployeesAsync(IFormFile file);
    }
}