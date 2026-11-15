using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text.RegularExpressions;

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
        public async Task<IActionResult> GetAll(
            [FromQuery] string name = null,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            // ✅ Kiểm tra giá trị hợp lý trước
            if (month.HasValue && (month.Value < 1 || month.Value > 12))
                return BadRequest("Tháng phải trong khoảng 1-12");
    
            if (year.HasValue && year.Value < 2000 || year.Value > DateTime.Now.Year + 1)
                return BadRequest("Năm không hợp lệ");

            var filter = Builders<Salary>.Filter.Empty;

         // Lọc theo tên
        if (!string.IsNullOrWhiteSpace(name))
        {
            filter &= Builders<Salary>.Filter.Regex(
                x => x.EmployeeName,
                new BsonRegularExpression(Regex.Escape(name.Trim()), "i"));
        }

        // Lọc theo năm 
        if (year.HasValue && year.Value > 0)
        {
            var yearFilter = Builders<Salary>.Filter.Or(
                Builders<Salary>.Filter.Eq(x => x.PeriodYear, year.Value),
                Builders<Salary>.Filter.And(
                    Builders<Salary>.Filter.Eq(x => x.PeriodYear, 0),
                    Builders<Salary>.Filter.Eq(x => x.Date.Year, year.Value)
                )
            );
            filter &= yearFilter;
        }

        // Lọc theo tháng 
        if (month.HasValue && month.Value > 0)
        {
            var monthFilter = Builders<Salary>.Filter.Or(
                Builders<Salary>.Filter.Eq(x => x.PeriodMonth, month.Value),
                Builders<Salary>.Filter.And(
                    Builders<Salary>.Filter.Eq(x => x.PeriodMonth, 0),
                    Builders<Salary>.Filter.Eq(x => x.Date.Month, month.Value)
                )
            );
            filter &= monthFilter;
        }

        var list = await _salaries.Find(filter).ToListAsync();
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

        [HttpPost("generate-month")]
        public async Task<IActionResult> GenerateMonth([FromBody] PayrollGenerateMonthRequest request)
        {
            try
            {
                if (request == null || request.Year <= 0 || request.Month < 1 || request.Month > 12)
                    return BadRequest(new { message = "Kỳ lương không hợp lệ (tháng 1-12, năm > 0)" });

                var generated = await _payrollService.GenerateMonthAsync(request);
                return Ok(new
                {
                    period = $"{request.Month:00}/{request.Year}",
                    generatedCount = generated.Count,
                    data = _mapper.Map<IEnumerable<SalaryDto>>(generated)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(
            [FromQuery] string name = null,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            var filter = Builders<Salary>.Filter.Empty;
            if (!string.IsNullOrWhiteSpace(name))
            {
                filter &= Builders<Salary>.Filter.Regex(
                    x => x.EmployeeName,
                    new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(name.Trim()), "i"));
            }
            if (year.HasValue && year.Value > 0)
            {
                filter &= Builders<Salary>.Filter.Or(
                    Builders<Salary>.Filter.Eq(x => x.PeriodYear, year.Value),
                    Builders<Salary>.Filter.Where(x => x.PeriodYear == 0 && x.Date.Year == year.Value)
                );
            }
            if (month.HasValue && month.Value > 0)
            {
                filter &= Builders<Salary>.Filter.Or(
                    Builders<Salary>.Filter.Eq(x => x.PeriodMonth, month.Value),
                    Builders<Salary>.Filter.Where(x => x.PeriodMonth == 0 && x.Date.Month == month.Value)
                );
            }

            var list = await _salaries.Find(filter).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Bảng lương");

            var headers = new[]
            {
                "STT", "Mã NV", "Họ và tên", "Kỳ lương", "Ngày công", "Lương CB", 
                "Phụ cấp", "Lương OT", "Thưởng", "Tổng thu nhập (Gross)", 
                "BHXH NLĐ", "BHYT NLĐ", "BHTN NLĐ", "Thuế TNCN", "Thực lĩnh (Net)", 
                "Chi phí CTY", "Ghi chú"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 73, 125));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                var row = i + 2;
                var period = $"{s.PeriodMonth:00}/{s.PeriodYear}";
                if (s.PeriodMonth == 0 && s.PeriodYear == 0 && s.Date != default)
                    period = $"{s.Date.Month:00}/{s.Date.Year}";

                ws.Cells[row, 1].Value = i + 1;
                ws.Cells[row, 2].Value = s.EmployeeId;
                ws.Cells[row, 3].Value = s.EmployeeName;
                ws.Cells[row, 4].Value = period;
                ws.Cells[row, 5].Value = s.WorkedDays;
                ws.Cells[row, 6].Value = s.BasicSalary;
                ws.Cells[row, 7].Value = s.Allowance;
                ws.Cells[row, 8].Value = s.OvertimePay;
                ws.Cells[row, 9].Value = s.Bonus;
                ws.Cells[row, 10].Value = s.GrossSalary;
                ws.Cells[row, 11].Value = s.BhxhEmployee;
                ws.Cells[row, 12].Value = s.BhytEmployee;
                ws.Cells[row, 13].Value = s.BhtnEmployee;
                ws.Cells[row, 14].Value = s.PersonalIncomeTax;
                ws.Cells[row, 15].Value = s.NetSalary;
                ws.Cells[row, 16].Value = s.CompanyCost;
                ws.Cells[row, 17].Value = s.SalaryNotes;

                for (int col = 6; col <= 16; col++)
                {
                    ws.Cells[row, col].Style.Numberformat.Format = "#,##0";
                }

                if (i % 2 == 1)
                {
                    using var range = ws.Cells[row, 1, row, headers.Length];
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 244, 250));
                }
            }

            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var bytes = await package.GetAsByteArrayAsync();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BangLuong_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        [HttpPost("add-salary")]
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

        [HttpDelete("delete-salary/{id}")]
        public async Task<IActionResult> DeleteSalaryLegacy(int id)
        {
            return await Delete(id);
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
                Allowance = dto.Allowance > 0 ? dto.Allowance : null,
                InsuranceSalary = dto.InsuranceSalary > 0 ? dto.InsuranceSalary : null,
                WorkedDays = dto.WorkedDays > 0 ? dto.WorkedDays : null,
                StandardDays = dto.StandardDays > 0 ? dto.StandardDays : null,
                UnpaidLeaveDays = dto.UnpaidLeaveDays,
                OvertimeHours = dto.OvertimeHours,
                Bonus = dto.Bonus,
                OtherDeductions = dto.OtherDeductions,
                DependentCount = dto.DependentCount,
                DeductUnionFee = dto.DeductUnionFee
            };
        }
    }
}