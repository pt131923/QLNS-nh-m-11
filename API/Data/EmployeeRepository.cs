using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using AutoMapper;
using MongoDB.Driver;
using OfficeOpenXml;
using System.Security.Cryptography;

namespace API.Data;

public class EmployeeRepository(
    IMongoDatabase database,
    IMongoCollection<AppDepartment> departments,
    IMongoCollection<FileHistory> fileHistory,
    IMongoIdGenerator idGenerator,
    IDashboardService dashboardService,
    IMapper mapper) : IEmployeeRepository
{
    // === Kết nối CSDL ===
    private readonly IMongoCollection<Employee> _employees = database.GetCollection<Employee>("Employees");
    private readonly IMongoCollection<AppDepartment> _departments = departments;
    private readonly IMongoCollection<FileHistory> _fileHistory = fileHistory;
    private readonly IMongoIdGenerator _idGenerator = idGenerator;
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly IMapper _mapper = mapper;

    private readonly List<Employee> _pendingInserts = [];
    private readonly List<Employee> _pendingUpdates = [];
    private readonly List<Employee> _pendingDeletes = [];

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync()
    {
        var employees = await _employees.Find(_ => true).ToListAsync();
        return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
    }

    public async Task<EmployeeDto> GetEmployeeByIdAsync(int id)
    {
        var employee = await _employees.Find(x => x.EmployeeId == id).FirstOrDefaultAsync();
        return employee == null ? null : _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<IEnumerable<EmployeeWithDepartmentDto>> GetWithDepartmentsAsync()
    {
        var employees = await _employees.Find(_ => true).ToListAsync();
        var deptList = await _departments.Find(_ => true).ToListAsync();
        var deptMap = deptList.ToDictionary(d => d.DepartmentId, d => d);

        foreach (var emp in employees)
        {
            if (deptMap.TryGetValue(emp.DepartmentId, out var dept))
                emp.Department = dept;
        }

        return _mapper.Map<IEnumerable<EmployeeWithDepartmentDto>>(employees);
    }

    public async Task<EmployeeDto> AddEmployeeAsync(EmployeeDto dto)
    {
        if (await IsNameExistsAsync(dto.EmployeeName))
            throw new ArgumentException("Employee name already exists");

        var deptExists = await _departments.CountDocumentsAsync(d => d.DepartmentId == dto.DepartmentId) > 0;
        if (!deptExists)
            throw new ArgumentException("Department does not exist");


        var employee = _mapper.Map<Employee>(dto);
        employee.EmployeeName = employee.EmployeeName.Trim().ToLower();

        Add(employee);
        if (!await SaveAllAsync())
            throw new InvalidOperationException("Failed to save employee");

        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<bool> UpdateEmployeeAsync(int id, EmployeeUpdateDto dto)
    {
        var employee = await _employees.Find(x => x.EmployeeId == id).FirstOrDefaultAsync();
        if (employee == null) return false;

        _mapper.Map(dto, employee);
        Update(employee);
        return await SaveAllAsync();
    }

	private void Update(Employee employee)
	{
		throw new NotImplementedException();
	}

	public async Task<bool> DeleteEmployeeAsync(int id)
    {
        var employee = await _employees.Find(x => x.EmployeeId == id).FirstOrDefaultAsync();
        if (employee == null) return false;

        Delete(employee);
        return await SaveAllAsync();
    }

    public async Task<(int total, int imported, List<string> errors)> ImportEmployeesAsync(IFormFile file)
    {
        var errors = new List<string>();
        var employeesToImport = new List<Employee>();
        var identityNumbersInFile = new HashSet<string>();

        // Kiểm tra file
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
            throw new ArgumentException("Only Excel files (.xlsx, .xls) are allowed");

        // Kiểm tra file đã upload trước đó chưa
        var fileHash = CalculateHash(file);
        if (await _fileHistory.CountDocumentsAsync(f => f.FileHash == fileHash) > 0)
            throw new ArgumentException("This file has been previously uploaded");

        // Đọc file Excel
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;
        using var package = new ExcelPackage(stream);
        var ws = package.Workbook.Worksheets[0];

        if (ws.Dimension == null)
            throw new ArgumentException("Excel file is empty");

        int rows = ws.Dimension.Rows;
        var headers = ReadHeaders(ws);

        // Duyệt từng dòng
        for (int row = 2; row <= rows; row++)
        {
            try
            {
                if (!TryGet(ws, headers, row, ["employeename", "employee name", "name"], out string name))
                {
                    errors.Add($"Row {row}: EmployeeName is required");
                    continue;
                }
                name = name.Trim().ToLower();
                if (await IsNameExistsAsync(name))
                {
                    errors.Add($"Row {row}: Employee name '{name}' already exists");
                    continue;
                }

                var emp = new Employee
                {
                    EmployeeName = name
                };

                if (TryGet(ws, headers, row, ["identitynumber", "identity number", "cccd"], out string cccd))
                {
                    cccd = cccd.Trim().ToLower();
                    if (identityNumbersInFile.Contains(cccd))
                    {
                        errors.Add($"Row {row}: Duplicate IdentityNumber in file");
                        continue;
                    }
                    if (await IsIdentityNumberExistsAsync(cccd))
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

        // Lưu dữ liệu
        if (employeesToImport.Count > 0)
        {
            foreach (var emp in employeesToImport)
                Add(emp);
            
            await SaveAllAsync();
        }

        // Lưu lịch sử file
        await _fileHistory.InsertOneAsync(new FileHistory
        {
            Id = await _idGenerator.NextAsync("FileHistory"),
            FileName = file.FileName,
            FileHash = fileHash,
            UploadedDate = DateTime.UtcNow,
            Status = errors.Count == 0 ? "SUCCESS" : "PARTIAL"
        });

        await _dashboardService.NotifyDataChangedWithCheckAsync();

        return (rows - 1, employeesToImport.Count, errors);
    }

    // === Hàm hỗ trợ ===
    private async Task<bool> IsNameExistsAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var cleaned = name.Trim().ToLower();
        var filter = Builders<Employee>.Filter.Regex(
            x => x.EmployeeName,
            new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(cleaned)}$", "i"));
        return await _employees.CountDocumentsAsync(filter) > 0;
    }

    private async Task<bool> IsIdentityNumberExistsAsync(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) return false;
        var clean = identity.Trim().ToLower();
        var filter = Builders<Employee>.Filter.Eq(x => x.IdentityNumber, clean);
        return await _employees.CountDocumentsAsync(filter) > 0;
    }

    private static string CalculateHash(IFormFile file)
    {
        using var sha = SHA256.Create();
        using var stream = file.OpenReadStream();
        var hash = sha.ComputeHashAsync(stream).Result;
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static Dictionary<string, int> ReadHeaders(ExcelWorksheet ws)
    {
        var headers = new Dictionary<string, int>();
        int cols = ws.Dimension.Columns;
        for (int col = 1; col <= cols; col++)
        {
            var value = ws.Cells[1, col].Value?.ToString()?.Trim().ToLower();
            if (!string.IsNullOrEmpty(value))
                headers[value] = col;
        }
        return headers;
    }

    private static bool TryGet(ExcelWorksheet ws, Dictionary<string, int> headers, int row, string[] keys, out string result)
    {
        foreach (var key in keys)
        {
            if (headers.TryGetValue(key, out int col))
            {
                result = ws.Cells[row, col].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(result)) return true;
            }
        }
        result = null!;
        return false;
    }

    public async Task<bool> SaveAllAsync()
    {
        var anyChanges = _pendingInserts.Count > 0 || _pendingUpdates.Count > 0 || _pendingDeletes.Count > 0;
        if (!anyChanges) return true;

        foreach (var d in _pendingDeletes)
            await _employees.DeleteOneAsync(x => x.EmployeeId == d.EmployeeId);

        foreach (var u in _pendingUpdates)
            await _employees.ReplaceOneAsync(x => x.EmployeeId == u.EmployeeId, u);

        if (_pendingInserts.Count > 0)
        {
            foreach (var i in _pendingInserts)
            {
                if (i.EmployeeId == 0)
                    i.EmployeeId = await _idGenerator.NextAsync("Employees");
                if (i.CreatedAt == default)
                    i.CreatedAt = DateTime.UtcNow;
            }
            await _employees.InsertManyAsync(_pendingInserts);
        }

        _pendingDeletes.Clear();
        _pendingUpdates.Clear();
        _pendingInserts.Clear();

        await _dashboardService.NotifyDataChangedWithCheckAsync();
        return true;
    }

	Task<IEnumerable<Employee>> IEmployeeRepository.GetEmployeesAsync()
	{
		throw new NotImplementedException();
	}

	Task<Employee> IEmployeeRepository.GetEmployeeByIdAsync(int id)
	{
		throw new NotImplementedException();
	}

	public Task<IEnumerable<EmployeeDto>> GetEmployeeAsync()
	{
		throw new NotImplementedException();
	}

	public void Add(Employee employee)
	{
		throw new NotImplementedException();
	}

	public void Delete(Employee employee)
	{
		throw new NotImplementedException();
	}

	public void Update(int id, Employee employee)
	{
		throw new NotImplementedException();
	}

	Task<bool> IEmployeeRepository.GetWithDepartmentsAsync()
	{
		throw new NotImplementedException();
	}
}