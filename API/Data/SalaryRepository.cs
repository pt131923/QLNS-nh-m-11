using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class SalaryRepository(DataContext _context, AutoMapper.IMapper _mapper, IDashboardService _dashboardService) : ISalaryRepository
    {

        public async Task<IEnumerable<Salary>> GetSalariesAsync()
        {
            return await _context.Salary
                .Include(s => s.Employee) // Nếu bạn muốn lấy cả thông tin nhân viên
                .ToArrayAsync();
        }

       public async Task<Salary> GetSalaryByIdAsync(int id)
       {
        return await _context.Salary
        .Include(s => s.Employee)
        .SingleOrDefaultAsync(x => x.SalaryId == id); // Đã sửa đúng
        }

        public async Task<IEnumerable<SalaryDto>> GetSalaryDtosAsync()
        {
            return await _context.Salary
                .ProjectTo<SalaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            var result = await _context.SaveChangesAsync() > 0;

            // Trigger cập nhật dashboard nếu có thay đổi dữ liệu lương
            if (result)
            {
                await _dashboardService.NotifyDataChangedWithCheckAsync();
            }

            return result;
        }

        public void Add(Salary salary)
        {
            _context.Salary.Add(salary);
        }

        public void Delete(Salary salary)
        {
            _context.Salary.Remove(salary);
        }

        public void Update(Salary salary)
        {
            _context.Entry(salary).State = EntityState.Modified;
        }
    }
}
