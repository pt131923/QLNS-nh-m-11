using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Security.Cryptography;
using MongoDB.Driver;
using API.Services;
using AutoMapper;
using System.Drawing;
using System.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController(
		IEmployeeRepository employeeRepository,
		IMapper mapper,
		IMongoCollection<Employee> employees,
		IMongoCollection<AppDepartment> departments,
		IMongoCollection<FileHistory> fileHistory,
		IMongoIdGenerator idGenerator,
		IDashboardService dashboardService) : BaseApiController
    {
        private readonly IMongoCollection<Employee> _employees = employees;
        private readonly IMongoCollection<AppDepartment> _departments = departments;
        private readonly IMongoCollection<FileHistory> _fileHistory = fileHistory;
        private readonly IMongoIdGenerator _idGenerator = idGenerator;
        private readonly IDashboardService _dashboardService = dashboardService;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly IMapper _mapper = mapper;

		[HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
        {
            var employees = await _employeeRepository.GetEmployeeAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employees));
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _employees.CountDocumentsAsync(_ => true);
            return Ok((int)count);
        }

        [HttpGet("{id}", Name = "GetEmployeeById")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeById(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();
            return Ok(_mapper.Map<EmployeeDto>(employee));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateEmployee(int id, EmployeeUpdateDto dto)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            _mapper.Map(dto, employee);
            _employeeRepository.Update(employee);

            if (await _employeeRepository.SaveAllAsync())
                return NoContent();

            return BadRequest("Failed to update employee");
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeDto>> AddEmployee(EmployeeDto employeeDto)
        {
            if (await EmployeeExists(employeeDto.EmployeeName))
                return BadRequest("Employee name already exists");

            var departmentExists = await _departments.CountDocumentsAsync(d => d.DepartmentId == employeeDto.DepartmentId) > 0;
            if (!departmentExists)
                return BadRequest("Department does not exist");

            var employee = _mapper.Map<Employee>(employeeDto);
            employee.EmployeeName = employee.EmployeeName.Trim().ToLower();
            _employeeRepository.Add(employee);
            await _employeeRepository.SaveAllAsync();

            return CreatedAtAction(nameof(GetEmployeeById),
                new { id = employee.EmployeeId },
                _mapper.Map<EmployeeDto>(employee));
        }

        private async Task<bool> EmployeeExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var cleaned = name.Trim().ToLower();
            var filter = Builders<Employee>.Filter.Regex(
                x => x.EmployeeName,
                new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleaned)}$", "i"));

            return await _employees.CountDocumentsAsync(filter) > 0;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEmployee(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            _employeeRepository.Delete(employee);
            if (await _employeeRepository.SaveAllAsync())
                return NoContent();

            return BadRequest("Failed to delete employee");
        }

        [HttpGet("with-departments")]
        public async Task<ActionResult<IEnumerable<EmployeeWithDepartmentDto>>> GetEmployeesWithDepartments()
        {
            var employees = await _employees.Find(_ => true).ToListAsync();
            var departments = await _departments.Find(_ => true).ToListAsync();
            var deptMap = departments.ToDictionary(d => d.DepartmentId, d => d);

            if (employees.Count == 0)
                return NotFound();

            foreach (var e in employees)
            {
                if (deptMap.TryGetValue(e.DepartmentId, out var dept))
                    e.Department = dept;
            }
 
            return Ok(employees.Select(_mapper.Map<EmployeeWithDepartmentDto>).ToList());
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> SearchEmployees(
            string name = null,
            int? departmentId = null,
            string gender = null,
            string educationLevel = null,
            string contractStatus = null)
        {
            var filter = Builders<Employee>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(name))
            {
                filter &= Builders<Employee>.Filter.Regex(
                    x => x.EmployeeName,
                    new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(name.Trim()), "i"));
            }

            if (departmentId.HasValue)
            {
                filter &= Builders<Employee>.Filter.Eq(x => x.DepartmentId, departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(gender))
            {
                filter &= Builders<Employee>.Filter.Regex(
                    x => x.Gender,
                    new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(gender.Trim())}$", "i"));
            }

            if (!string.IsNullOrWhiteSpace(educationLevel))
            {
                filter &= Builders<Employee>.Filter.Regex(
                    x => x.EducationLevel,
                    new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(educationLevel.Trim())}$", "i"));
            }

            var employeeList = await _employees.Find(filter).ToListAsync();

            // Filter by contract status if requested
            if (!string.IsNullOrWhiteSpace(contractStatus))
            {
                var contractCol = _employees.Database.GetCollection<Contract>("Contracts");
                var today = DateTime.UtcNow;
                var empNames = employeeList.Select(e => e.EmployeeName?.ToLower()).Where(n => n != null).ToList();

                var contracts = await contractCol
                    .Find(Builders<Contract>.Filter.In(c => c.EmployeeName, empNames))
                    .ToListAsync();

                var contractMap = contracts
                    .GroupBy(c => c.EmployeeName?.ToLower())
                    .ToDictionary(g => g.Key ?? "", g => g.ToList());

                employeeList = employeeList.Where(emp =>
                {
                    var key = emp.EmployeeName?.ToLower() ?? "";
                    if (!contractMap.TryGetValue(key, out var empContracts) || empContracts.Count == 0)
                        return contractStatus.Equals("none", StringComparison.OrdinalIgnoreCase);

                    var active = empContracts.Any(c => c.StartDate <= today && c.EndDate >= today);
                    return contractStatus.Equals("active", StringComparison.OrdinalIgnoreCase) ? active : !active;
                }).ToList();
            }

            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employeeList));
        }

        [HttpPost("batch-update-department")]
        public async Task<ActionResult> BatchUpdateDepartment([FromBody] BatchDepartmentUpdateDto dto)
        {
            if (dto == null || dto.EmployeeIds == null || dto.EmployeeIds.Count == 0)
                return BadRequest("No employee IDs provided");

            var deptExists = await _departments.CountDocumentsAsync(d => d.DepartmentId == dto.NewDepartmentId) > 0;
            if (!deptExists)
                return BadRequest("Department does not exist");

            var update = Builders<Employee>.Update.Set(x => x.DepartmentId, dto.NewDepartmentId);
            var filter = Builders<Employee>.Filter.In(x => x.EmployeeId, dto.EmployeeIds);
            var result = await _employees.UpdateManyAsync(filter, update);

            await _dashboardService.NotifyDataChangedWithCheckAsync();

            return Ok(new { updated = result.ModifiedCount });
        }

        [HttpGet("statistics")]
        public async Task<ActionResult> GetStatistics()
        {
            var allEmployees = await _employees.Find(_ => true).ToListAsync();
            var allDepartments = await _departments.Find(_ => true).ToListAsync();
            var deptMap = allDepartments.ToDictionary(d => d.DepartmentId, d => d.Name);

            // By Department
            var byDepartment = allEmployees
                .GroupBy(e => e.DepartmentId)
                .Select(g => new
                {
                    departmentId = g.Key,
                    departmentName = deptMap.TryGetValue(g.Key, out var n) ? n : $"Dept {g.Key}",
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToList();

            // By Gender
            var byGender = allEmployees
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Gender) ? "Chưa xác định" : e.Gender)
                .Select(g => new { gender = g.Key, count = g.Count() })
                .ToList();

            // By Age Group
            var today = DateTime.UtcNow;
            var byAgeGroup = allEmployees
                .Where(e => e.BirthDate.HasValue)
                .Select(e => (int)((today - e.BirthDate.Value).TotalDays / 365.25))
                .GroupBy(age => age < 25 ? "Dưới 25" : age < 35 ? "25–34" : age < 45 ? "35–44" : age < 55 ? "45–54" : "55+")
                .Select(g => new { ageGroup = g.Key, count = g.Count() })
                .ToList();

            return Ok(new { byDepartment, byGender, byAgeGroup, total = allEmployees.Count });
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(
            string name = null,
            int? departmentId = null,
            string gender = null,
            string educationLevel = null)
        {
            var filter = Builders<Employee>.Filter.Empty;
            if (!string.IsNullOrWhiteSpace(name))
                filter &= Builders<Employee>.Filter.Regex(x => x.EmployeeName,
                    new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(name.Trim()), "i"));
            if (departmentId.HasValue)
                filter &= Builders<Employee>.Filter.Eq(x => x.DepartmentId, departmentId.Value);
            if (!string.IsNullOrWhiteSpace(gender))
                filter &= Builders<Employee>.Filter.Regex(x => x.Gender,
                    new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(gender.Trim())}$", "i"));
            if (!string.IsNullOrWhiteSpace(educationLevel))
                filter &= Builders<Employee>.Filter.Regex(x => x.EducationLevel,
                    new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(educationLevel.Trim())}$", "i"));

            var employeeList = await _employees.Find(filter).ToListAsync();
            var depts = await _departments.Find(_ => true).ToListAsync();
            var deptMap = depts.ToDictionary(d => d.DepartmentId, d => d.Name);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Danh sách nhân viên");

            // Header styling
            var headers = new[] { "STT", "Mã NV", "Họ và tên", "Phòng ban", "Email", "Điện thoại", "Giới tính", "Ngày sinh", "Địa chỉ", "CCCD", "Trình độ", "Chuyên ngành" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 73, 125));
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Data rows
            for (int i = 0; i < employeeList.Count; i++)
            {
                var emp = employeeList[i];
                var row = i + 2;
                ws.Cells[row, 1].Value = i + 1;
                ws.Cells[row, 2].Value = emp.EmployeeId;
                ws.Cells[row, 3].Value = emp.EmployeeName;
                ws.Cells[row, 4].Value = deptMap.TryGetValue(emp.DepartmentId, out var dn) ? dn : emp.DepartmentId.ToString();
                ws.Cells[row, 5].Value = emp.EmployeeEmail;
                ws.Cells[row, 6].Value = emp.EmployeePhone;
                ws.Cells[row, 7].Value = emp.Gender;
                ws.Cells[row, 8].Value = emp.BirthDate?.ToString("dd/MM/yyyy");
                ws.Cells[row, 9].Value = emp.EmployeeAddress;
                ws.Cells[row, 10].Value = emp.IdentityNumber;
                ws.Cells[row, 11].Value = emp.EducationLevel;
                ws.Cells[row, 12].Value = emp.Specialization;

                if (i % 2 == 1)
                {
                    using var range = ws.Cells[row, 1, row, headers.Length];
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(217, 225, 242));
                }
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var bytes = await package.GetAsByteArrayAsync();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DanhSachNhanVien_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        [HttpPost("import")]
        public async Task<ActionResult> ImportEmployees(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest("Only Excel files (.xlsx, .xls) are allowed");

            var errors = new List<string>();
            var employeesToImport = new List<Employee>();
            var identityNumbersInFile = new HashSet<string>();

            var fileHash = await CalculateHash(file);
            if (await _fileHistory.CountDocumentsAsync(f => f.FileHash == fileHash) > 0)
                return BadRequest("This file has been previously uploaded");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];

            if (ws.Dimension == null)
                return BadRequest("Excel file is empty");

            int rows = ws.Dimension.Rows;
            int cols = ws.Dimension.Columns;

            var headers = new Dictionary<string, int>();
            for (int col = 1; col <= cols; col++)
            {
                var value = ws.Cells[1, col].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    headers[value.ToLower()] = col;
            }

            for (int row = 2; row <= rows; row++)
            {
                try
                {
                    var emp = new Employee();

                    if (!TryGet(ws, headers, row, ["employeename", "employee name", "name"], out string name))
                    {
                        errors.Add($"Row {row}: EmployeeName is required");
                        continue;
                    }

                    emp.EmployeeName = name.Trim().ToLower();

                    if (await EmployeeExists(emp.EmployeeName))
                    {
                        errors.Add($"Row {row}: Employee name '{name}' already exists");
                        continue;
                    }

                    if (TryGet(ws, headers, row, ["identitynumber", "identity number", "cccd"], out string cccd))
                    {
                        cccd = cccd.Trim().ToLower();

                        if (identityNumbersInFile.Contains(cccd))
                        {
                            errors.Add($"Row {row}: Duplicate IdentityNumber in file");
                            continue;
                        }

                        if (await IdentityNumberExists(cccd))
                        {
                            errors.Add($"Row {row}: IdentityNumber '{cccd}' already exists in DB");
                            continue;
                        }

                        identityNumbersInFile.Add(cccd);
                        emp.IdentityNumber = cccd;
                    }

                    if (!TryGet(ws, headers, row, ["departmentid", "department id", "department"], out string deptValue))
                    {
                        errors.Add($"Row {row}: DepartmentId/Department is required");
                        continue;
                    }

                    if (int.TryParse(deptValue.Trim(), out int deptId))
                    {
                        var exists = await _departments.CountDocumentsAsync(d => d.DepartmentId == deptId) > 0;
                        if (!exists)
                        {
                            errors.Add($"Row {row}: DepartmentId '{deptId}' not found");
                            continue;
                        }
                        emp.DepartmentId = deptId;
                    }
                    else
                    {
                        var dept = await _departments.Find(
                            Builders<AppDepartment>.Filter.Regex(
                                x => x.Name,
                                new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(deptValue.Trim())}$", "i")))
                            .FirstOrDefaultAsync();

                        if (dept == null)
                        {
                            errors.Add($"Row {row}: Department '{deptValue}' not found");
                            continue;
                        }
                        emp.DepartmentId = dept.DepartmentId;
                    }

                    emp.EmployeeId = await _idGenerator.NextAsync("Employees");
                    emp.CreatedAt = DateTime.UtcNow;
                    employeesToImport.Add(emp);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {row}: {ex.Message}");
                }
            }

            if (employeesToImport.Count > 0)
                await _employees.InsertManyAsync(employeesToImport);

            await _fileHistory.InsertOneAsync(new FileHistory
            {
                Id = await _idGenerator.NextAsync("FileHistory"),
                FileName = file.FileName,
                FileHash = fileHash,
                UploadedDate = DateTime.UtcNow,
                Status = errors.Count == 0 ? "SUCCESS" : "PARTIAL"
            });

            await _dashboardService.NotifyDataChangedWithCheckAsync();

            return Ok(new
            {
                total = rows - 1,
                imported = employeesToImport.Count,
                errors
            });
        }

        private static async Task<string> CalculateHash(IFormFile file)
        {
            using var sha = SHA256.Create();
            using var stream = file.OpenReadStream();
            var hash = await sha.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private async Task<bool> IdentityNumberExists(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                return false;

            var clean = identity.Trim().ToLower();
            var filter = Builders<Employee>.Filter.Eq(x => x.IdentityNumber, clean);
            return await _employees.CountDocumentsAsync(filter) > 0;
        }

        private static bool TryGet(ExcelWorksheet ws, Dictionary<string, int> headers, int row, string[] keys, out string result)
        {
            foreach (var key in keys)
            {
                if (headers.TryGetValue(key, out int col))
                {
                    result = ws.Cells[row, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(result))
                        return true;
                }
            }
            result = null!;
            return false;
        }
    }
}