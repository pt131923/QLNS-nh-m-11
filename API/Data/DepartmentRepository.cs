using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly DataContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public DepartmentRepository(DataContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

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
            return await _context.SaveChangesAsync() > 0;
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