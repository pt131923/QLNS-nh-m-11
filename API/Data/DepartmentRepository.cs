using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DepartmentRepository(DataContext _context, AutoMapper.IMapper _mapper, IDashboardService _dashboardService) : IDepartmentRepository
    {
        public async Task<IEnumerable<AppDepartment>> GetDepartmentsAsync()
        {
            return await _context.Department
                .ToArrayAsync();
        }


        public async Task<AppDepartment> GetDepartmentByIdAsync(int id)
        {
            return await _context.Department
                .SingleOrDefaultAsync(x => x.DepartmentId == id);
        }

        public async Task<IEnumerable<DepartmentDto>> GetDepartmentAsync()
        {
            return await _context.Department
                .ProjectTo<DepartmentDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            var result = await _context.SaveChangesAsync() > 0;

            // Trigger cập nhật dashboard nếu có thay đổi dữ liệu
            if (result)
            {
                await _dashboardService.NotifyDataChangedWithCheckAsync();
            }

            return result;
        }

        public void Add(AppDepartment depart)
        {
            _context.Department.Add(depart);
        }

        public void Delete(AppDepartment depart)
        {
            _context.Department.Remove(depart);
        }

        public void Update(AppDepartment depart)
        {
            _context.Entry(depart).State = EntityState.Modified;
        }

        public async Task<bool> DepartmentExists(string departmentName)
        {
            return await _context.Department.AnyAsync(x => x.Name.Equals(departmentName, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}