using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

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

        [HttpPost("import-salaries")]
        public async Task<ActionResult> ImportSalaries(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required");
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Only Excel files (.xlsx, .xls) are allowed");
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet.Dimension == null)
                {
                    return BadRequest("Excel file is empty");
                }

                var rowCount = worksheet.Dimension.Rows;
                var colCount = worksheet.Dimension.Columns;

                if (rowCount < 2)
                {
                    return BadRequest("Excel file must have at least a header row and one data row");
                }

                // Read header row to map columns
                var headers = new Dictionary<string, int>();
                for (int col = 1; col <= colCount; col++)
                {
                    var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        headers[headerValue.ToLowerInvariant()] = col;
                    }
                }

                var importedCount = 0;
                var errorMessages = new List<string>();

                // Process data rows
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var salary = new Salary();

                        // Map EmployeeId - can be employee name or ID
                        int? employeeId = null;
                        if (headers.ContainsKey("employeeid") || headers.ContainsKey("employee id"))
                        {
                            var col = headers.ContainsKey("employeeid") ? headers["employeeid"] : headers["employee id"];
                            var value = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (int.TryParse(value, out int empId))
                                {
                                    employeeId = empId;
                                }
                                else
                                {
                                    // Try to find employee by name
                                    var employee = await _context.Employee
                                        .FirstOrDefaultAsync(e => e.EmployeeName.ToLower() == value.ToLower());
                                    if (employee != null)
                                    {
                                        employeeId = employee.EmployeeId;
                                    }
                                    else
                                    {
                                        errorMessages.Add($"Row {row}: Employee '{value}' not found");
                                        continue;
                                    }
                                }
                            }
                        }
                        else if (headers.ContainsKey("employeename") || headers.ContainsKey("employee name") || headers.ContainsKey("name"))
                        {
                            var col = headers.ContainsKey("employeename") ? headers["employeename"] :
                                     headers.ContainsKey("employee name") ? headers["employee name"] : headers["name"];
                            var value = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                var employee = await _context.Employee
                                    .FirstOrDefaultAsync(e => e.EmployeeName.ToLower() == value.ToLower());
                                if (employee != null)
                                {
                                    employeeId = employee.EmployeeId;
                                    salary.EmployeeName = employee.EmployeeName;
                                }
                                else
                                {
                                    errorMessages.Add($"Row {row}: Employee '{value}' not found");
                                    continue;
                                }
                            }
                        }

                        if (!employeeId.HasValue)
                        {
                            errorMessages.Add($"Row {row}: EmployeeId or EmployeeName is required");
                            continue;
                        }

                        // Check if employee exists
                        var employeeExists = await _context.Employee.AnyAsync(e => e.EmployeeId == employeeId.Value);
                        if (!employeeExists)
                        {
                            errorMessages.Add($"Row {row}: Employee with ID {employeeId.Value} does not exist");
                            continue;
                        }

                        salary.EmployeeId = employeeId.Value;

                        // Get employee name if not already set
                        if (string.IsNullOrEmpty(salary.EmployeeName))
                        {
                            var employee = await _context.Employee.FindAsync(employeeId.Value);
                            if (employee != null)
                            {
                                salary.EmployeeName = employee.EmployeeName;
                            }
                        }

                        // Map MonthlySalary
                        if (headers.ContainsKey("monthlysalary") || headers.ContainsKey("monthly salary") || headers.ContainsKey("salary"))
                        {
                            var col = headers.ContainsKey("monthlysalary") ? headers["monthlysalary"] :
                                     headers.ContainsKey("monthly salary") ? headers["monthly salary"] : headers["salary"];
                            var value = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (decimal.TryParse(value, out decimal monthlySalary))
                                {
                                    salary.MonthlySalary = monthlySalary;
                                }
                                else
                                {
                                    errorMessages.Add($"Row {row}: Invalid MonthlySalary value");
                                    continue;
                                }
                            }
                        }

                        // Map Bonus
                        MapExcelCell(worksheet, row, headers, "bonus", "", value =>
                        {
                            if (decimal.TryParse(value, out decimal bonus))
                            {
                                salary.Bonus = bonus;
                            }
                        });

                        // Map TotalSalary (if provided, otherwise calculate)
                        if (headers.ContainsKey("totalsalary") || headers.ContainsKey("total salary") || headers.ContainsKey("total"))
                        {
                            var col = headers.ContainsKey("totalsalary") ? headers["totalsalary"] :
                                     headers.ContainsKey("total salary") ? headers["total salary"] : headers["total"];
                            var value = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (decimal.TryParse(value, out decimal totalSalary))
                                {
                                    salary.TotalSalary = totalSalary;
                                }
                            }
                        }
                        else
                        {
                            // Calculate TotalSalary if not provided
                            salary.TotalSalary = salary.MonthlySalary + salary.Bonus;
                        }

                        // Map SalaryNotes
                        MapExcelCell(worksheet, row, headers, "salarynotes", "salary notes", "notes", value => salary.SalaryNotes = value);

                        // Map Date
                        if (headers.ContainsKey("date"))
                        {
                            var col = headers["date"];
                            var value = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (DateTime.TryParse(value, out DateTime date))
                                {
                                    salary.Date = date;
                                }
                                else
                                {
                                    errorMessages.Add($"Row {row}: Invalid Date format");
                                    continue;
                                }
                            }
                            else
                            {
                                salary.Date = DateTime.Now;
                            }
                        }
                        else
                        {
                            salary.Date = DateTime.Now;
                        }

                        _context.Salary.Add(salary);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"Row {row}: Error - {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                var response = new
                {
                    success = true,
                    importedCount = importedCount,
                    totalRows = rowCount - 1,
                    errors = errorMessages
                };

                if (errorMessages.Count > 0)
                {
                    return Ok(response);
                }

                return Ok(new { success = true, message = $"Successfully imported {importedCount} salaries", importedCount });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing Excel file: {ex.Message}");
            }
        }

        private void MapExcelCell(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string key1, string key2, Action<string> setValue)
        {
            if (headers.ContainsKey(key1))
            {
                var value = worksheet.Cells[row, headers[key1]].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    setValue(value);
            }
            else if (headers.ContainsKey(key2))
            {
                var value = worksheet.Cells[row, headers[key2]].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    setValue(value);
            }
        }

        private void MapExcelCell(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string key1, string key2, string key3, Action<string> setValue)
        {
            if (headers.ContainsKey(key1))
            {
                var value = worksheet.Cells[row, headers[key1]].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    setValue(value);
            }
            else if (headers.ContainsKey(key2))
            {
                var value = worksheet.Cells[row, headers[key2]].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    setValue(value);
            }
            else if (headers.ContainsKey(key3))
            {
                var value = worksheet.Cells[row, headers[key3]].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    setValue(value);
            }
        }
    }
}
