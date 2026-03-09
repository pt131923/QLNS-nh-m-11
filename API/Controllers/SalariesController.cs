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
    public class SalariesController : BaseApiController
    {
        private readonly ISalaryRepository _salaryRepository;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IMongoCollection<Employee> _employees;
        private readonly IMongoCollection<Salary> _salaries;
        private readonly IMongoIdGenerator _idGenerator;
        private readonly IDashboardService _dashboardService;

        public SalariesController(
            ISalaryRepository salaryRepository,
            AutoMapper.IMapper mapper,
            IMongoDatabase database,
            IMongoIdGenerator idGenerator,
            IDashboardService dashboardService)
        {
            _salaryRepository = salaryRepository;
            _mapper = mapper;
            _employees = database.GetCollection<Employee>("Employees");
            _salaries = database.GetCollection<Salary>("Salaries");
            _idGenerator = idGenerator;
            _dashboardService = dashboardService;
        }

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
            var employeeExists = await _employees.CountDocumentsAsync(e => e.EmployeeId == salaryDto.EmployeeId) > 0;
            if (!employeeExists)
                return BadRequest("Employee does not exist.");

            var employee = await _employees.Find(e => e.EmployeeId == salaryDto.EmployeeId).FirstOrDefaultAsync();
            var salary = _mapper.Map<Salary>(salaryDto);
            salary.EmployeeName = employee?.EmployeeName ?? salary.EmployeeName;

            _salaryRepository.Add(salary);
            await _salaryRepository.SaveAllAsync();

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
                var employeeExists = await _employees.CountDocumentsAsync(e => e.EmployeeId == salaryDto.EmployeeId) > 0;
                if (!employeeExists)
                    return BadRequest($"New Employee with ID {salaryDto.EmployeeId} does not exist.");
            }

            var employee = await _employees.Find(e => e.EmployeeId == salaryDto.EmployeeId).FirstOrDefaultAsync();
            _mapper.Map(salaryDto, existingSalary);
            existingSalary.EmployeeName = employee?.EmployeeName ?? existingSalary.EmployeeName;

            _salaryRepository.Update(existingSalary);
            if (await _salaryRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update salary");
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
    }
}
