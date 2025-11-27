using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class DepartmentsController(DataContext _context, IDepartmentRepository _departmentRepository, AutoMapper.IMapper _mapper) : BaseApiController
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
        {
            var departments = await _departmentRepository.GetDepartmentAsync();
            return Ok(departments);
        }

        [HttpGet("{id}", Name = "GetDepartmentById")]
        public async Task<ActionResult<DepartmentDto>> GetDepartmentById(int id)
        {
            var department = await _departmentRepository.GetDepartmentByIdAsync(id);

            if (department == null)
                return NotFound();

            return Ok(department);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDepartment(int id, DepartmentUpdateDto departmentUpdateDto)
        {
            var department = await _departmentRepository.GetDepartmentByIdAsync(id);

            if (department == null)
                return NotFound();

            _mapper.Map(departmentUpdateDto, department);
            _departmentRepository.Update(department);

            if (await _departmentRepository.SaveAllAsync())
                return NoContent();

            return BadRequest("Failed to update Department");
        }

        [HttpPost("add-department")]
        public async Task<ActionResult<DepartmentDto>> AddDepartment(DepartmentDto departmentDto)
        {
            if (departmentDto == null || string.IsNullOrWhiteSpace(departmentDto.Name))
                return BadRequest("Department name is required");

            if (await DepartmentExists(departmentDto.Name))
                return BadRequest("Department name is already taken");

            var department = _mapper.Map<AppDepartment>(departmentDto);

            _context.Department.Add(department);
            await _context.SaveChangesAsync();

            return CreatedAtRoute(
                "GetDepartmentById",
                new { id = department.DepartmentId },
                _mapper.Map<DepartmentDto>(department)
            );
        }

        [HttpDelete("delete-department/{id}")]
        public async Task<ActionResult> DeleteDepartment(int id)
        {
            var department = await _departmentRepository.GetDepartmentByIdAsync(id);

            if (department == null)
                return NotFound();

            _departmentRepository.Delete(department);

            if (await _departmentRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Failed to delete the Department");
        }

        private async Task<bool> DepartmentExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return await _context.Department.AnyAsync(d => d.Name.ToLower() == name.ToLower());
        }
    }
}


