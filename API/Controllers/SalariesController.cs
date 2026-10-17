using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalariesController(
		ISalaryRepository salaryRepo,
		IMapper mapper,
		IMongoCollection<Employee> employees,
		IMongoCollection<Salary> salaries,
		IPayrollService payrollService) : BaseApiController
    {
        // ---- Khai báo các dịch vụ cần dùng ----
        private readonly ISalaryRepository _salaryRepo = salaryRepo;
        private readonly IMapper _mapper = mapper;
        private readonly IMongoCollection<Employee> _employees = employees;
        private readonly IMongoCollection<Salary> _salaries = salaries;
        private readonly IPayrollService _payrollService = payrollService;

		[HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _salaryRepo.GetSalariesAsync();
            return Ok(_mapper.Map<IEnumerable<SalaryDto>>(list));
        }

        [HttpGet("{id}", Name = "GetSalaryById")]
        public async Task<IActionResult> GetById(int id)
        {
            var salary = await _salaryRepo.GetSalaryByIdAsync(id);
            if (salary == null) return NotFound("Không tìm thấy bản ghi lương");
            
            return Ok(_mapper.Map<SalaryDto>(salary));
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate(PayrollCalculateRequest request)
        {
            try
            {
                var result = await _payrollService.BuildAsync(request);
                
                if (request.Save)
                {
                    var saved = await _payrollService.UpsertFromResultAsync(result, $"Bảng lương {result.Month:00}/{result.Year}");
                    return Ok(_mapper.Map<SalaryDto>(saved));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add(SalaryDto dto)
        {
            try
            {
                var request = ToPayrollRequest(dto);
                var calcResult = await _payrollService.BuildAsync(request);
                var newSalary = await _payrollService.UpsertFromResultAsync(calcResult, dto.SalaryNotes);
                
                return CreatedAtRoute("GetSalaryById", new { id = newSalary.SalaryId }, 
                                      _mapper.Map<SalaryDto>(newSalary));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(SalaryDto dto, int id)
        {
            var existing = await _salaryRepo.GetSalaryByIdAsync(id);
            if (existing == null) return NotFound("Không tìm thấy bản ghi");

            try
            {
                // Giữ EmployeeId nếu không truyền
                dto.EmployeeId = dto.EmployeeId > 0 ? dto.EmployeeId : existing.EmployeeId;
                
                var request = ToPayrollRequest(dto);
                var result = await _payrollService.BuildAsync(request);
                result.EmployeeId = dto.EmployeeId;
                
                var updated = await _payrollService.UpsertFromResultAsync(result, dto.SalaryNotes);
                return Ok(_mapper.Map<SalaryDto>(updated));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var salary = await _salaryRepo.GetSalaryByIdAsync(id);
            if (salary == null) return NotFound("Không tìm thấy bản ghi");
            
            _salaryRepo.Delete(salary);
            await _salaryRepo.SaveAllAsync();
            
            return Ok(new { message = "Xóa thành công" });
        }

        private static PayrollCalculateRequest ToPayrollRequest(SalaryDto dto)
        {
            var date = dto.Date == default ? DateTime.UtcNow : dto.Date;
            
            return new PayrollCalculateRequest
            {
                EmployeeId = dto.EmployeeId,
                Year = dto.PeriodYear > 0 ? dto.PeriodYear : date.Year,
                Month = dto.PeriodMonth > 0 ? dto.PeriodMonth : date.Month,
                BasicSalary = dto.BasicSalary > 0 ? dto.BasicSalary : dto.MonthlySalary,
                Bonus = dto.Bonus,
                DependentCount = dto.DependentCount
            };
        }
    }
}
