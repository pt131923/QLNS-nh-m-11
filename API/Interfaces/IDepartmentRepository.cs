using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<bool> SaveAllAsync();
        Task<IEnumerable<AppDepartment>> GetDepartmentsAsync();
        Task<AppDepartment> GetDepartmentByIdAsync(int id);
        Task<IEnumerable<DepartmentDto>> GetDepartmentAsync();
        void Add(AppDepartment department);
        void Delete(AppDepartment department);
        void Update(AppDepartment department);
        Task<bool> DepartmentExists(string departmentName);
    }
}