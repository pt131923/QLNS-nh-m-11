using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Security.Cryptography;
using MongoDB.Driver;
using API.Services;
using System.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : BaseApiController
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IMongoCollection<Employee> _employees;
        private readonly IMongoCollection<AppDepartment> _departments;
        private readonly IMongoCollection<FileHistory> _fileHistory;
        private readonly IMongoIdGenerator _idGenerator;
        private readonly IDashboardService _dashboardService;

        public EmployeesController(
            IEmployeeRepository employeeRepository,
            AutoMapper.IMapper mapper,
            IMongoDatabase database,
            IMongoIdGenerator idGenerator,
            IDashboardService dashboardService)
        {
            _employeeRepository = employeeRepository;
            _mapper = mapper;
            _employees = database.GetCollection<Employee>("Employees");
            _departments = database.GetCollection<AppDepartment>("Departments");
            _fileHistory = database.GetCollection<FileHistory>("FileHistory");
            _idGenerator = idGenerator;
            _dashboardService = dashboardService;
        }

        // ---------------------------------------------------------------------
        // GET ALL EMPLOYEES
        // ---------------------------------------------------------------------
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
        // ---------------------------------------------------------------------
        // GET EMPLOYEE BY ID
        // ---------------------------------------------------------------------
        [HttpGet("{id}", Name = "GetEmployeeById")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeById(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

            if (employee == null)
                return NotFound();

            return Ok(_mapper.Map<EmployeeDto>(employee));
        }

        // ---------------------------------------------------------------------
        // UPDATE EMPLOYEE
        // ---------------------------------------------------------------------
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

        // ---------------------------------------------------------------------
        // ADD EMPLOYEE (CREATE)
        // ---------------------------------------------------------------------
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
            if (string.IsNullOrWhiteSpace(name)) return false;
            var cleaned = name.Trim().ToLower();
            var filter = Builders<Employee>.Filter.Regex(x => x.EmployeeName, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleaned)}$", "i"));
            return await _employees.CountDocumentsAsync(filter) > 0;
        }

        // ---------------------------------------------------------------------
        // DELETE EMPLOYEE
        // ---------------------------------------------------------------------
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

        // ---------------------------------------------------------------------
        // GET EMPLOYEES + DEPARTMENT
        // ---------------------------------------------------------------------
        [HttpGet("with-departments")]
        public async Task<ActionResult<IEnumerable<EmployeeWithDepartmentDto>>> GetEmployeesWithDepartments()
        {
            var employees = await _employees.Find(_ => true).ToListAsync();
            var departments = await _departments.Find(_ => true).ToListAsync();
            var deptMap = departments.ToDictionary(d => d.DepartmentId, d => d);

            foreach (var e in employees)
                if (deptMap.TryGetValue(e.DepartmentId, out var dept))
                    e.Department = dept;

            return Ok(employees.Select(e => _mapper.Map<EmployeeWithDepartmentDto>(e)).ToList());
        }

        // ---------------------------------------------------------------------
        // SEARCH EMPLOYEE
        // ---------------------------------------------------------------------
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> SearchEmployees(
            string name = null,
            int? departmentId = null)
        {
            var filter = Builders<Employee>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(name))
                filter &= Builders<Employee>.Filter.Regex(x => x.EmployeeName, new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(name.Trim()), "i"));

            if (departmentId.HasValue)
                filter &= Builders<Employee>.Filter.Eq(x => x.DepartmentId, departmentId.Value);

            var employees = await _employees.Find(filter).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employees));
        }

        // ---------------------------------------------------------------------
        // IMPORT EMPLOYEE FROM EXCEL
        // ---------------------------------------------------------------------
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

            // Compute file hash
            var fileHash = await CalculateHash(file);
            if (await _fileHistory.CountDocumentsAsync(f => f.FileHash == fileHash) > 0)
                return BadRequest("This file has been previously uploaded");

            // Read Excel
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

            // Build header dictionary
            var headers = new Dictionary<string, int>();
            for (int col = 1; col <= cols; col++)
            {
                var value = ws.Cells[1, col].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    headers[value.ToLower()] = col;
            }

            // Read rows
            for (int row = 2; row <= rows; row++)
            {
                try
                {
                    var emp = new Employee();

                    // EmployeeName REQUIRED
                    if (!TryGet(ws, headers, row, ["employeename", "employee name", "name"], out string name))
                    {
                        errors.Add($"Row {row}: EmployeeName is required");
                        continue;
                    }

                    emp.EmployeeName = name.ToLower();

                    if (await EmployeeExists(emp.EmployeeName))
                    {
                        errors.Add($"Row {row}: Employee name '{name}' already exists");
                        continue;
                    }

                    // IdentityNumber
                    if (TryGet(ws, headers, row, ["identitynumber", "identity number", "cccd"], out string cccd))
                    {
                        cccd = cccd.ToLower();
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

                    // DepartmentId
                    if (!TryGet(ws, headers, row, ["departmentid", "department id", "department"], out string deptValue))
                    {
                        errors.Add($"Row {row}: DepartmentId/Department is required");
                        continue;
                    }

                    if (int.TryParse(deptValue, out int deptId))
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

            // Save employees + history
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

        // ---------------------------------------------------------------------
        // HELPERS
        // ---------------------------------------------------------------------
        private async Task<string> CalculateHash(IFormFile file)
        {
            using var sha = SHA256.Create();
            using var stream = file.OpenReadStream();
            var hash = await sha.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private async Task<bool> IdentityNumberExists(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity)) return false;
            var clean = identity.Trim().ToLower();
            var filter = Builders<Employee>.Filter.Eq(x => x.IdentityNumber, clean);
            return await _employees.CountDocumentsAsync(filter) > 0;
        }

        private bool TryGet(ExcelWorksheet ws, Dictionary<string, int> headers, int row, IEnumerable<string> keys, out string result)
        {
            foreach (var key in keys)
            {
                if (headers.TryGetValue(key, out int col))
                {
                    result = ws.Cells[row, col].Value?.ToString()?.Trim();
                    return !string.IsNullOrEmpty(result);
                }
            }
            result = null;
            return false;
        }
    }
}
