using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(IEmployeeRepository employeeRepository) : BaseApiController
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        var data = await _employeeRepository.GetEmployeesAsync();
        return Ok(data);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        return Ok(await _employeeRepository.GetCountAsync());
    }

    [HttpGet("{id}", Name = "GetEmployeeById")]
    public async Task<IActionResult> GetById(int id)
    {
        var emp = await _employeeRepository.GetEmployeeByIdAsync(id);
        if (emp == null) return NotFound();
        return Ok(emp);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> AddEmployee(EmployeeDto dto)
    {
        try
        {
            var result = await _employeeRepository.AddEmployeeAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.EmployeeId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, EmployeeUpdateDto dto)
    {
        var ok = await _employeeRepository.UpdateEmployeeAsync(id, dto);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var ok = await _employeeRepository.DeleteEmployeeAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpGet("with-departments")]
    public async Task<IActionResult> GetWithDepartments()
    {
        var data = await _employeeRepository.GetWithDepartmentsAsync();
        return Ok(data);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        try
        {
            var result = await _employeeRepository.ImportEmployeesAsync(file);
            return Ok(new { result.total, result.imported, result.errors });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}