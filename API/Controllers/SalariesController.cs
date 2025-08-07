using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalariesController(DataContext _context, ISalaryRepository _salaryRepository, AutoMapper.IMapper _mapper) : BaseApiController
        {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalaryDto>>> GetSalaries()
        {
            var salaries = await _salaryRepository.GetSalariesAsync();
            return Ok(_mapper.Map<IEnumerable<SalaryDto>>(salaries));
        }

        [HttpGet("{id}", Name = "GetSalaryById")]
        public async Task<ActionResult<SalaryDto>> GetSalaryById(int id)
        {
            var salary = await _salaryRepository.GetSalaryByIdAsync(id);
            if (salary == null) return NotFound("Salary not found.");

            return Ok(_mapper.Map<SalaryDto>(salary));
        }

        [HttpPost("add-salary")]
        public async Task<ActionResult<SalaryDto>> AddSalary(SalaryDto salaryDto)
        {
            var employeeExists = await _context.Employee.AnyAsync(e => e.EmployeeId == salaryDto.EmployeeId);
            if (!employeeExists)
                return BadRequest("Employee with SalaryId does not exist.");

            var salary = _mapper.Map<Salary>(salaryDto);

            _context.Salary.Add(salary);
            await _context.SaveChangesAsync();

            return CreatedAtRoute("GetSalaryById", new { id = salary.SalaryId }, _mapper.Map<SalaryDto>(salary));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateSalary(SalaryDto salaryDto, int id)
        {
            var existingSalary = await _salaryRepository.GetSalaryByIdAsync(id);
            if (existingSalary == null)
                return NotFound("Salary not found.");

            if (salaryDto.EmployeeId != existingSalary.EmployeeId)
            {
                var employeeExists = await _context.Employee.AnyAsync(e => e.EmployeeId == salaryDto.EmployeeId);
                if (!employeeExists)
                    return BadRequest($"New Employee with ID {salaryDto.EmployeeId} does not exist.");
            }

            _mapper.Map(salaryDto, existingSalary);
            _salaryRepository.Update(existingSalary);

            if (await _salaryRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update salary");
        }

        [HttpDelete("delete-salary/{id}")]
        public IActionResult DeleteSalary(int id)
        {
            var salary = _context.Salary.Find(id);
            if (salary == null)
                return NotFound(new { message = "Salary not found." });

            _context.Salary.Remove(salary);
            _context.SaveChanges();

            return Ok(new { message = "Salary deleted successfully." });
        }
    }
}
