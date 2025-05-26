using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class SalaryRepository : ISalaryRepository
    {
        private readonly DataContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public SalaryRepository(DataContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

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
            return await _context.SaveChangesAsync() > 0;
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
