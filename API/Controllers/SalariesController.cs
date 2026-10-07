using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OfficeOpenXml;
using System.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalariesController(ISalaryRepository _salaryRepository, AutoMapper.IMapper _mapper, IMongoCollection<Employee> _employees, IMongoCollection<Salary> _salaries, IMongoIdGenerator _idGenerator, IDashboardService _dashboardService, IPayrollService _payrollService) : BaseApiController
    {
        private readonly IMongoCollection<Employee> _employees = _employees;
        private readonly IMongoCollection<Salary> _salaries = _salaries;
        private readonly IMongoIdGenerator _idGenerator = _idGenerator;
        private readonly IDashboardService _dashboardService = _dashboardService;
        private readonly IPayrollService _payrollService = _payrollService;
        private readonly ISalaryRepository _salaryRepository = _salaryRepository;
        private readonly AutoMapper.IMapper _mapper = _mapper;
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalaryDto>>> GetSalaries()
        {
            var salaries = await _salaryRepository.GetSalariesAsync();
            return Ok(_mapper.Map<IEnumerable<SalaryDto>>(salaries));
        }

        [HttpGet("payroll-rates")]
        public ActionResult GetPayrollRates()
        {
            return Ok(new
            {
                BhxhEmployeeRate = PayrollCalculator.BhxhEmployeeRate,
                BhytEmployeeRate = PayrollCalculator.BhytEmployeeRate,
                BhtnEmployeeRate = PayrollCalculator.BhtnEmployeeRate,
                UnionEmployeeRate = PayrollCalculator.UnionEmployeeRate,
                BhxhEmployerRate = PayrollCalculator.BhxhEmployerRate,
                BhytEmployerRate = PayrollCalculator.BhytEmployerRate,
                BhtnEmployerRate = PayrollCalculator.BhtnEmployerRate,
                UnionEmployerRate = PayrollCalculator.UnionEmployerRate,
                BaseWage = PayrollCalculator.BaseWage,
                RegionalMinimumWageRegion1 = PayrollCalculator.RegionalMinimumWageRegion1,
                BhxhBhytCeiling = PayrollCalculator.BhxhBhytCeiling,
                BhtnCeiling = PayrollCalculator.BhtnCeiling,
                PersonalDeduction = PayrollCalculator.PersonalDeduction,
                DependentDeduction = PayrollCalculator.DependentDeduction
            });
        }

        [HttpGet("{id}", Name = "GetSalaryById")]
        public async Task<ActionResult<SalaryDto>> GetSalaryById(int id)
        {
            var salary = await _salaryRepository.GetSalaryByIdAsync(id);
            if (salary == null) return NotFound("Salary not found.");

            return Ok(_mapper.Map<SalaryDto>(salary));
        }

        [HttpPost("calculate")]
        public async Task<ActionResult<PayrollResult>> CalculatePayroll(PayrollCalculateRequest request)
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
        public async Task<ActionResult> GenerateMonth(PayrollGenerateMonthRequest request)
        {
            if (request.Year <= 0 || request.Month is < 1 or > 12)
                return BadRequest(new { message = "Năm/tháng không hợp lệ." });

            try
            {
                var saved = await _payrollService.GenerateMonthAsync(request);
                return Ok(new
                {
                    generatedCount = saved.Count,
                    period = $"{request.Month:00}/{request.Year}",
                    items = _mapper.Map<IEnumerable<SalaryDto>>(saved)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("add-salary")]
        public async Task<ActionResult<SalaryDto>> AddSalary(SalaryDto salaryDto)
        {
            try
            {
                var result = await _payrollService.BuildAsync(ToPayrollRequest(salaryDto));
                var salary = await _payrollService.UpsertFromResultAsync(result, salaryDto.SalaryNotes);
                return CreatedAtRoute("GetSalaryById", new { id = salary.SalaryId }, _mapper.Map<SalaryDto>(salary));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateSalary(SalaryDto salaryDto, int id)
        {
            var existingSalary = await _salaryRepository.GetSalaryByIdAsync(id);
            if (existingSalary == null)
                return NotFound("Salary not found.");

            try
            {
                salaryDto.EmployeeId = salaryDto.EmployeeId > 0 ? salaryDto.EmployeeId : existingSalary.EmployeeId;
                var result = await _payrollService.BuildAsync(ToPayrollRequest(salaryDto));
                result.EmployeeId = salaryDto.EmployeeId;
                var updated = await _payrollService.UpsertFromResultAsync(result, salaryDto.SalaryNotes);
                return Ok(_mapper.Map<SalaryDto>(updated));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("delete-salary/{id}")]
        public async Task<IActionResult> DeleteSalary(int id)
        {
            var salary = await _salaryRepository.GetSalaryByIdAsync(id);
            if (salary == null)
                return NotFound(new { message = "Salary not found." });

            _salaryRepository.Delete(salary);
            await _salaryRepository.SaveAllAsync();

            return Ok(new { message = "Salary deleted successfully." });
        }

        [HttpPost("import-salaries")]
        public async Task<ActionResult> ImportSalaries(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Only Excel files (.xlsx, .xls) are allowed");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet.Dimension == null)
                return BadRequest("Excel file is empty");

            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;
            if (rowCount < 2)
                return BadRequest("Excel file must have at least a header row and one data row");

            var headers = new Dictionary<string, int>();
            for (int col = 1; col <= colCount; col++)
            {
                var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(headerValue))
                    headers[headerValue.ToLowerInvariant()] = col;
            }

            var importedCount = 0;
            var errorMessages = new List<string>();
            var salariesToInsert = new List<Salary>();

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var salary = new Salary();

                    int? employeeId = null;
                    if (TryGetCell(worksheet, row, headers, out var empValue, "employeeid", "employee id"))
                    {
                        if (int.TryParse(empValue, out var empId))
                        {
                            employeeId = empId;
                        }
                        else
                        {
                            var employee = await _employees.Find(Builders<Employee>.Filter.Regex(x => x.EmployeeName,
                                new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(empValue)}$", "i"))).FirstOrDefaultAsync();
                            if (employee == null)
                            {
                                errorMessages.Add($"Row {row}: Employee '{empValue}' not found");
                                continue;
                            }
                            employeeId = employee.EmployeeId;
                            salary.EmployeeName = employee.EmployeeName;
                        }
                    }
                    else if (TryGetCell(worksheet, row, headers, out var empName, "employeename", "employee name", "name"))
                    {
                        var employee = await _employees.Find(Builders<Employee>.Filter.Regex(x => x.EmployeeName,
                            new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(empName)}$", "i"))).FirstOrDefaultAsync();
                        if (employee == null)
                        {
                            errorMessages.Add($"Row {row}: Employee '{empName}' not found");
                            continue;
                        }
                        employeeId = employee.EmployeeId;
                        salary.EmployeeName = employee.EmployeeName;
                    }

                    if (!employeeId.HasValue)
                    {
                        errorMessages.Add($"Row {row}: EmployeeId or EmployeeName is required");
                        continue;
                    }

                    var employeeExists = await _employees.CountDocumentsAsync(e => e.EmployeeId == employeeId.Value) > 0;
                    if (!employeeExists)
                    {
                        errorMessages.Add($"Row {row}: Employee with ID {employeeId.Value} does not exist");
                        continue;
                    }

                    salary.EmployeeId = employeeId.Value;
                    if (string.IsNullOrEmpty(salary.EmployeeName))
                    {
                        var employee = await _employees.Find(e => e.EmployeeId == employeeId.Value).FirstOrDefaultAsync();
                        salary.EmployeeName = employee?.EmployeeName;
                    }

                    if (TryGetCell(worksheet, row, headers, out var monthlySalaryValue, "monthlysalary", "monthly salary", "salary"))
                    {
                        if (!decimal.TryParse(monthlySalaryValue, out var monthlySalary))
                        {
                            errorMessages.Add($"Row {row}: Invalid MonthlySalary value");
                            continue;
                        }
                        salary.MonthlySalary = monthlySalary;
                    }

                    if (TryGetCell(worksheet, row, headers, out var bonusValue, "bonus"))
                        if (decimal.TryParse(bonusValue, out var bonus))
                            salary.Bonus = bonus;

                    if (TryGetCell(worksheet, row, headers, out var totalValue, "totalsalary", "total salary", "total"))
                        if (decimal.TryParse(totalValue, out var totalSalary))
                            salary.TotalSalary = totalSalary;
                    if (salary.TotalSalary == 0)
                        salary.TotalSalary = salary.MonthlySalary + salary.Bonus;

                    if (TryGetCell(worksheet, row, headers, out var notesValue, "salarynotes", "salary notes", "notes"))
                        salary.SalaryNotes = notesValue;

                    if (TryGetCell(worksheet, row, headers, out var dateValue, "date") && DateTime.TryParse(dateValue, out var date))
                        salary.Date = date;
                    if (salary.Date == default)
                        salary.Date = DateTime.UtcNow;

                    try
                    {
                        var computed = await _payrollService.BuildAsync(new PayrollCalculateRequest
                        {
                            EmployeeId = salary.EmployeeId,
                            Year = salary.Date.Year,
                            Month = salary.Date.Month,
                            BasicSalary = salary.MonthlySalary > 0 ? salary.MonthlySalary : null,
                            Bonus = salary.Bonus,
                            DependentCount = 0
                        });
                        salary.PeriodYear = computed.Year;
                        salary.PeriodMonth = computed.Month;
                        salary.BasicSalary = computed.BasicSalary;
                        salary.Allowance = computed.Allowance;
                        salary.InsuranceSalary = computed.InsuranceSalary;
                        salary.StandardDays = computed.StandardDays;
                        salary.WorkedDays = computed.WorkedDays;
                        salary.UnpaidLeaveDays = computed.UnpaidLeaveDays;
                        salary.OvertimeHours = computed.OvertimeHours;
                        salary.OvertimePay = computed.OvertimePay;
                        salary.Bonus = computed.Bonus;
                        salary.GrossSalary = computed.GrossSalary;
                        salary.BhxhEmployee = computed.BhxhEmployee;
                        salary.BhytEmployee = computed.BhytEmployee;
                        salary.BhtnEmployee = computed.BhtnEmployee;
                        salary.UnionFeeEmployee = computed.UnionFeeEmployee;
                        salary.TotalInsuranceEmployee = computed.TotalInsuranceEmployee;
                        salary.PersonalDeductionAmount = computed.PersonalDeductionAmount;
                        salary.DependentDeduction = computed.DependentDeduction;
                        salary.TaxableIncome = computed.TaxableIncome;
                        salary.PersonalIncomeTax = computed.PersonalIncomeTax;
                        salary.EmployerBhxh = computed.EmployerBhxh;
                        salary.EmployerBhyt = computed.EmployerBhyt;
                        salary.EmployerBhtn = computed.EmployerBhtn;
                        salary.EmployerUnion = computed.EmployerUnion;
                        salary.TotalEmployerInsurance = computed.TotalEmployerInsurance;
                        salary.CompanyCost = computed.CompanyCost;
                        salary.NetSalary = computed.NetSalary;
                        salary.MonthlySalary = computed.GrossSalary;
                        salary.TotalSalary = computed.NetSalary;
                        salary.Amount = (long)computed.NetSalary;
                        salary.FormulaNotes = computed.FormulaNotes;
                    }
                    catch
                    {
                        salary.TotalSalary = salary.MonthlySalary + salary.Bonus;
                    }

                    salary.SalaryId = await _idGenerator.NextAsync("Salaries");
                    salariesToInsert.Add(salary);
                    importedCount++;
                }
                catch (Exception ex)
                {
                    errorMessages.Add($"Row {row}: Error - {ex.Message}");
                }
            }

            if (salariesToInsert.Count > 0)
                await _salaries.InsertManyAsync(salariesToInsert);

            await _dashboardService.NotifyDataChangedWithCheckAsync();

            return Ok(new
            {
                success = errorMessages.Count == 0,
                importedCount,
                totalRows = rowCount - 1,
                errors = errorMessages
            });
        }

        private static bool TryGetCell(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, out string value, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (headers.TryGetValue(key, out var col))
                {
                    value = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(value)) return true;
                }
            }
            value = null;
            return false;
        }

        private static PayrollCalculateRequest ToPayrollRequest(SalaryDto dto)
        {
            var date = dto.Date == default ? DateTime.UtcNow : dto.Date;
            var year = dto.PeriodYear > 0 ? dto.PeriodYear : date.Year;
            var month = dto.PeriodMonth > 0 ? dto.PeriodMonth : date.Month;
            var basic = dto.BasicSalary > 0 ? dto.BasicSalary : dto.MonthlySalary;

            return new PayrollCalculateRequest
            {
                EmployeeId = dto.EmployeeId,
                Year = year,
                Month = month,
                BasicSalary = basic > 0 ? basic : null,
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
