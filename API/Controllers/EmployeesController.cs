using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Security.Cryptography; // Cần thiết cho SHA256 Hash

namespace API.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController(DataContext _context, IEmployeeRepository _employeeRepository, AutoMapper.IMapper _mapper) : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
        {
            var employees = await _employeeRepository.GetEmployeeAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employees));
        }

        [HttpGet("{id}", Name = "GetEmployeeById")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeById(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<EmployeeDto>(employee));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateEmployee(EmployeeUpdateDto employeeUpdateDto, int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            _mapper.Map(employeeUpdateDto, employee);
            _employeeRepository.Update(employee);

            if (await _employeeRepository.SaveAllAsync())
                return NoContent();

            return BadRequest("Failed to update employee");
        }

        [HttpPost("add-employee")]
        public async Task<ActionResult<EmployeeDto>> AddEmployee([FromBody] EmployeeDto employeeDto)
        {
            if (await EmployeeExists(employeeDto.EmployeeName))
                return BadRequest("Employee Name is taken");

            var departmentExists = await _context.Department.AnyAsync(d => d.DepartmentId == employeeDto.DepartmentId);
            if (!departmentExists)
                return BadRequest("Department does not exist");

            var employee = _mapper.Map<Employee>(employeeDto);
            employee.EmployeeName = employee.EmployeeName.Trim().ToLower();

            _context.Employee.Add(employee);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<EmployeeDto>(employee);

            // ✅ Trả về đúng theo chuẩn RESTful
            return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.EmployeeId }, result);
        }

        private async Task<bool> EmployeeExists(string employeeName)
        {
            return await _context.Employee.AnyAsync(e => e.EmployeeName.ToLower() == employeeName.Trim().ToLower());
        }

        [HttpDelete("delete-employee/{id}")]
        public async Task<ActionResult> DeleteEmployee(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

            if (employee == null)
                return NotFound();

            _employeeRepository.Delete(employee);

            if (await _employeeRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Failed to delete the employee");
        }

        [HttpGet("employees-with-departments")]
        public async Task<ActionResult<IEnumerable<EmployeeWithDepartmentDto>>> GetEmployeesWithDepartments()
        {
            var employees = await _context.Employee
                .Include(e => e.Department)
                .Select(e => _mapper.Map<EmployeeWithDepartmentDto>(e))
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> SearchEmployees(string Name = null, int? departmentId = null)
        {
            var query = _context.Employee.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                query = query.Where(e => e.EmployeeName.ToLower().Contains(Name.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(Name))
            {
                query = query.Where(e => e.DepartmentId == departmentId);
            }

            var employees = await query.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employees));
        }
        
        // --- CÁC HÀM HỖ TRỢ ---

        private async Task<string> CalculateHash(IFormFile file)
        {
            using var sha256 = SHA256.Create();
            // Cần OpenReadStream để tính Hash
            using var stream = file.OpenReadStream(); 
            var hashBytes = await sha256.ComputeHashAsync(stream);
            stream.Position = 0; // Đặt lại vị trí để hàm gọi có thể đọc lại file
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private async Task<bool> IdentityNumberExists(string identityNumber)
        {
            if (string.IsNullOrWhiteSpace(identityNumber)) return false; 
            var cleanedId = identityNumber.Trim().ToLower();
            
            // Kiểm tra trùng lặp CCCD/IdentityNumber
            return await _context.Employee.AnyAsync(e => 
                !string.IsNullOrEmpty(e.IdentityNumber) && 
                e.IdentityNumber.ToLower() == cleanedId);
        }


        // --- HÀM IMPORT ĐÃ CẬP NHẬT LOGIC ---

        [HttpPost("import-employees")]
        public async Task<ActionResult> ImportEmployees(IFormFile file)
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
            
            var errorMessages = new List<string>();
            string fileHash = string.Empty;

            try
            {
                // **BƯỚC 1: Xử lý và kiểm tra Hash File**
                using var fileStream = new MemoryStream();
                await file.CopyToAsync(fileStream);
                
                // Tính Hash từ stream (sẽ được đặt lại vị trí 0 sau khi tính)
                fileHash = await CalculateHash(new FormFile(fileStream, 0, fileStream.Length, file.Name, file.FileName)); 

                // Kiểm tra file có bị trùng lặp không
                if (await _context.FileHistory.AnyAsync(f => f.FileHash == fileHash))
                {
                    return BadRequest("Lỗi: File Excel này đã được tải lên và xử lý trước đó.");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                fileStream.Position = 0; // Đặt lại vị trí đầu stream để đọc Excel
                
                using var package = new ExcelPackage(fileStream);
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

                var employeesToImport = new List<Employee>();
                var importedCount = 0;
                var identityNumbersInFile = new HashSet<string>(); // Chặn trùng lặp ID nội bộ file

                // **BƯỚC 2: Xử lý và kiểm tra từng dòng dữ liệu**
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var employee = new Employee();

                        // Map IdentityNumber
                        MapExcelCell(worksheet, row, headers, "identitynumber", "identity number", "cccd", value => employee.IdentityNumber = value);

                        // Map EmployeeName (Required)
                        var nameSet = false;
                        if (headers.ContainsKey("employeename") || headers.ContainsKey("employee name") || headers.ContainsKey("name"))
                        {
                            var col = headers.ContainsKey("employeename") ? headers["employeename"] :
                                       headers.ContainsKey("employee name") ? headers["employee name"] : headers["name"];
                            var value = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(value))
                            {
                                errorMessages.Add($"Row {row}: EmployeeName is required");
                                continue;
                            }
                            employee.EmployeeName = value.Trim().ToLower();
                            nameSet = true;
                        }
                        if (!nameSet)
                        {
                            errorMessages.Add($"Row {row}: EmployeeName column not found");
                            continue;
                        }

                        // **Kiểm tra trùng lặp CCCD/IdentityNumber (Database và Nội bộ File)** ⚠️
                        if (!string.IsNullOrWhiteSpace(employee.IdentityNumber))
                        {
                            var cleanedId = employee.IdentityNumber.Trim().ToLower();
                            
                            // Kiểm tra trùng lặp nội bộ
                            if (identityNumbersInFile.Contains(cleanedId))
                            {
                                errorMessages.Add($"Row {row}: IdentityNumber '{employee.IdentityNumber}' bị trùng lặp trong file Excel này.");
                                continue;
                            }
                            
                            // Kiểm tra trùng lặp trong DB
                            if (await IdentityNumberExists(employee.IdentityNumber))
                            {
                                errorMessages.Add($"Row {row}: IdentityNumber '{employee.IdentityNumber}' đã tồn tại trong hệ thống (DB).");
                                continue;
                            }
                            
                            identityNumbersInFile.Add(cleanedId);
                        }

                        // Kiểm tra trùng lặp Employee Name (Nếu bạn vẫn muốn kiểm tra)
                        if (await EmployeeExists(employee.EmployeeName))
                        {
                            errorMessages.Add($"Row {row}: Employee Name '{employee.EmployeeName}' đã tồn tại");
                            continue;
                        }

                        // Map DepartmentId
                        bool departmentIdSet = false;
                        // ... (Code Map DepartmentId và kiểm tra Department như cũ) ...
                        if (headers.ContainsKey("departmentid") || headers.ContainsKey("department id") || headers.ContainsKey("department"))
                        {
                            var col = headers.ContainsKey("departmentid") ? headers["departmentid"] :
                                       headers.ContainsKey("department id") ? headers["department id"] : headers["department"];
                            var value = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (int.TryParse(value, out int deptId))
                                {
                                    employee.DepartmentId = deptId;
                                    departmentIdSet = true;
                                }
                                else
                                {
                                    var department = await _context.Department
                                        .FirstOrDefaultAsync(d => d.Name.ToLower() == value.ToLower());
                                    if (department != null)
                                    {
                                        employee.DepartmentId = department.DepartmentId;
                                        departmentIdSet = true;
                                    }
                                    else
                                    {
                                        errorMessages.Add($"Row {row}: Department '{value}' not found");
                                        continue;
                                    }
                                }
                            }
                        }
                        
                         if (!departmentIdSet || employee.DepartmentId == 0)
                         {
                             errorMessages.Add($"Row {row}: DepartmentId or Department is required");
                             continue;
                         }
                        
                        var departmentExists = await _context.Department.AnyAsync(d => d.DepartmentId == employee.DepartmentId);
                        if (!departmentExists)
                        {
                            errorMessages.Add($"Row {row}: DepartmentId {employee.DepartmentId} does not exist");
                            continue;
                        }

                        // Map other fields
                        MapExcelCell(worksheet, row, headers, "employeeemail", "employee email", "email", value => employee.EmployeeEmail = value);
                        MapExcelCell(worksheet, row, headers, "employeephone", "employee phone", "phone", value => employee.EmployeePhone = value);
                        MapExcelCell(worksheet, row, headers, "employeeaddress", "employee address", "address", value => employee.EmployeeAddress = value);
                        MapExcelCell(worksheet, row, headers, "employeeinformation", "employee information", "information", value => employee.EmployeeInformation = value);
                        MapExcelCell(worksheet, row, headers, "birthdate", "birth date", "birthdate", value => 
                        {
                            if (DateTime.TryParse(value, out DateTime date)) employee.BirthDate = date;
                        });
                        MapExcelCell(worksheet, row, headers, "placeofbirth", "place of birth", "placeofbirth", value => employee.PlaceOfBirth = value);
                        MapExcelCell(worksheet, row, headers, "gender", "", value => employee.Gender = value);
                        MapExcelCell(worksheet, row, headers, "maritalstatus", "marital status", value => employee.MaritalStatus = value);
                        MapExcelCell(worksheet, row, headers, "identityissueddate", "identity issued date", value =>
                        {
                            if (DateTime.TryParse(value, out DateTime date)) employee.IdentityIssuedDate = date;
                        });
                        MapExcelCell(worksheet, row, headers, "identityissuedplace", "identity issued place", value => employee.IdentityIssuedPlace = value);
                        MapExcelCell(worksheet, row, headers, "religion", "", value => employee.Religion = value);
                        MapExcelCell(worksheet, row, headers, "ethnicity", "", value => employee.Ethnicity = value);
                        MapExcelCell(worksheet, row, headers, "nationality", "", value => employee.Nationality = value);
                        MapExcelCell(worksheet, row, headers, "educationlevel", "education level", value => employee.EducationLevel = value);
                        MapExcelCell(worksheet, row, headers, "specialization", "", value => employee.Specialization = value);

                        employeesToImport.Add(employee);
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"Row {row}: Error - {ex.Message}");
                    }
                }

                // **BƯỚC 3: Lưu dữ liệu và lịch sử file**
                _context.Employee.AddRange(employeesToImport);
                importedCount = employeesToImport.Count;
                
                // Lưu lịch sử file
                var history = new FileHistory
                {
                    FileName = file.FileName,
                    FileHash = fileHash,
                    UploadedDate = DateTime.UtcNow,
                    Status = errorMessages.Count == 0 ? "SUCCESS" : "PARTIAL"
                };
                _context.FileHistory.Add(history);

                await _context.SaveChangesAsync();
                
                // **BƯỚC 4: Phản hồi kết quả**
                var response = new
                {
                    success = errorMessages.Count == 0,
                    importedCount = importedCount,
                    totalRows = rowCount - 1,
                    errors = errorMessages
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing Excel file: {ex.Message}");
            }
        }

        // --- CÁC HÀM HỖ TRỢ MAP CELL (Giữ nguyên) ---
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