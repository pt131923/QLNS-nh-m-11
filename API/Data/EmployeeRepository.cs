using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly DataContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public EmployeeRepository(DataContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

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
            return await _context.SaveChangesAsync() > 0;
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