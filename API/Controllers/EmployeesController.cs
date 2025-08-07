using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController(DataContext _context, IEmployeeRepository _employeeRepository, AutoMapper.IMapper _mapper) : BaseApiController
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
        {
            var employees = await _employeeRepository.GetEmployeeAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employees));
        }

        [HttpGet("{id}", Name = "GetEmployeeById")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeById(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<EmployeeDto>(employee));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateEmployee(EmployeeUpdateDto employeeUpdateDto, int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            _mapper.Map(employeeUpdateDto, employee);
            _employeeRepository.Update(employee);

            if (await _employeeRepository.SaveAllAsync())
                return NoContent();

            return BadRequest("Failed to update employee");
        }

        [HttpPost("add-employee")]
        public async Task<ActionResult<EmployeeDto>> AddEmployee([FromBody] EmployeeDto employeeDto)
        {
            if (await EmployeeExists(employeeDto.EmployeeName))
                return BadRequest("Employee Name is taken");

            var departmentExists = await _context.Department.AnyAsync(d => d.DepartmentId == employeeDto.DepartmentId);
            if (!departmentExists)
                return BadRequest("Department does not exist");

            var employee = _mapper.Map<Employee>(employeeDto);
            employee.EmployeeName = employee.EmployeeName.Trim().ToLower();

            _context.Employee.Add(employee);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<EmployeeDto>(employee);

            // ✅ Trả về đúng theo chuẩn RESTful
            return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.EmployeeId }, result);
        }

        private async Task<bool> EmployeeExists(string employeeName)
        {
            return await _context.Employee.AnyAsync(e => e.EmployeeName.ToLower() == employeeName.Trim().ToLower());
        }

        [HttpDelete("delete-employee/{id}")]
        public async Task<ActionResult> DeleteEmployee(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

            if (employee == null)
                return NotFound();

            _employeeRepository.Delete(employee);

            if (await _employeeRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Failed to delete the employee");
        }

        [HttpGet("employees-with-departments")]
        public async Task<ActionResult<IEnumerable<EmployeeWithDepartmentDto>>> GetEmployeesWithDepartments()
        {
            var employees = await _context.Employee
                .Include(e => e.Department)
                .Select(e => _mapper.Map<EmployeeWithDepartmentDto>(e))
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> SearchEmployees(string Name = null, int? departmentId = null)
        {
            var query = _context.Employee.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                query = query.Where(e => e.EmployeeName.ToLower().Contains(Name.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(Name))
            {
                query = query.Where(e => e.DepartmentId == departmentId);
            }

            var employees = await query.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employees));
        }
    }
}
