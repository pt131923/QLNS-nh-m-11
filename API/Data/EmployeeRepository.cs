using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API.Data
{
    public class EmployeeRepository(DataContext _context, AutoMapper.IMapper _mapper, IDashboardService _dashboardService) : IEmployeeRepository
    {

        public async Task<IEnumerable<Employee>> GetEmployeesAsync()
        {
            return await _context.Employee
                .ToArrayAsync();
        }


        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employee
                .SingleOrDefaultAsync(x => x.EmployeeId == id);
        }

        public async Task<IEnumerable<EmployeeDto>> GetEmployeeAsync()
        {
            return await _context.Employee
                .ProjectTo<EmployeeDto>(_mapper.ConfigurationProvider)
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


        public void Add(Employee employee)
        {
            _context.Employee.Add(employee);
        }

        public void Delete(Employee employee)
        {
            _context.Employee.Remove(employee);
        }

        public void Update(Employee employee)
        {
            _context.Entry(employee).State = EntityState.Modified;
        }
    }
}