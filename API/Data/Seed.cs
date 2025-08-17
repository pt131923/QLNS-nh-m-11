using System.Text.Json;
using API.DTOs;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedDepartments(DataContext context)
        {
            if (await context.Department.AnyAsync()) return;

            var departmentData = await File.ReadAllTextAsync("Data/DepartmentSeedData.json");
            var departments = JsonSerializer.Deserialize<List<AppDepartment>>(departmentData);

            if (departments != null)
            {
                await context.Department.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Seeded Department data.");
        }

        public static async Task SeedEmployees(DataContext context)
{
    if (await context.Employee.AnyAsync()) return;

    var employeeData = await File.ReadAllTextAsync("Data/EmployeeSeedData.json");

    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    var employeeDtos = JsonSerializer.Deserialize<List<EmployeeSeedDto>>(employeeData, options);

    if (employeeDtos == null || !employeeDtos.Any())
    {
        Console.WriteLine("⚠ Không có dữ liệu Employee trong file JSON.");
        return;
    }

    var departments = await context.Department.ToListAsync();
    if (departments == null || !departments.Any())
    {
        Console.WriteLine("⚠ Không thể seed Employees vì chưa có Department.");
        return;
    }

    var employees = new List<Employee>();

    foreach (var dto in employeeDtos)
    {
        var departmentDto = dto.Department;
        if (departmentDto == null)
        {
            Console.WriteLine("⚠ Dữ liệu Department trong DTO bị null.");
            continue;
        }

        var department = departments.FirstOrDefault(dep => dep.Name == departmentDto.Name);

        if (department == null)
        {
            Console.WriteLine($"⚠ Không tìm thấy phòng ban với tên: {departmentDto}");
            continue;
        }

        var employee = new Employee
        {
            EmployeeName = dto.EmployeeName,
            EmployeeEmail = dto.EmployeeEmail,
            EmployeePhone = dto.EmployeePhone,
            EmployeeAddress = dto.EmployeeAddress,
            DepartmentId = department.DepartmentId,
            BirthDate = dto.BirthDate,
            PlaceOfBirth = dto.PlaceOfBirth,
            Gender = dto.Gender,
            MaritalStatus = dto.MaritalStatus,
            IdentityNumber = dto.IdentityNumber,
            IdentityIssuedDate = dto.IdentityIssuedDate,
            IdentityIssuedPlace = dto.IdentityIssuedPlace,
            Religion = dto.Religion,
            Ethnicity = dto.Ethnicity,
            Nationality = dto.Nationality,
            EducationLevel = dto.EducationLevel,
            Specialization = dto.Specialization
        };

        employees.Add(employee);
    }

    await context.Employee.AddRangeAsync(employees);
    await context.SaveChangesAsync();

    Console.WriteLine("✅ Seeded Employee data.");
}



        public static async Task SeedContracts(DataContext context)
        {
            if (await context.Contract.AnyAsync()) return;

            var employees = await context.Employee.Include(e => e.Contracts).ToListAsync();
            if (employees == null || !employees.Any())
            {
                Console.WriteLine("⚠ Cannot seed Contracts because Employees are missing.");
                return;
            }

            var contractData = await File.ReadAllTextAsync("Data/ContractSeedData.json");
            var contracts = JsonSerializer.Deserialize<List<Contract>>(contractData);

            if (contracts != null)
            {
                foreach (var contract in contracts)
                {
                    // Gán EmployeeId ngẫu nhiên
                    var randomEmployee = employees[System.Security.Cryptography.RandomNumberGenerator.GetInt32(employees.Count)];

                    // Gán mặc định nếu cần
                    contract.BasicSalary = contract.BasicSalary == 0 ? 8000000 : contract.BasicSalary;
                    contract.Allowance = contract.Allowance == 0 ? 1000000 : contract.Allowance;
                    contract.JobDescription ??= "Chưa cập nhật mô tả công việc";
                    contract.WorkLocation ??= "Không rõ địa chỉ";
                    contract.Leaveofabsence ??= "Không có";

                    // Gán thời gian mặc định nếu cần
                    contract.CreateAt = contract.CreateAt == default ? DateTime.Now : contract.CreateAt;
                    contract.UpdateAt = contract.UpdateAt == default ? DateTime.Now : contract.UpdateAt;
                    contract.ContractTerm = contract.ContractTerm == default
                        ? contract.EndDate.ToString()
                        : contract.ContractTerm;

                    // Ràng buộc hợp đồng cho nhân viên
                    randomEmployee.Contracts ??= new List<Contract>();
                    randomEmployee.Contracts.Add(contract);
                }

                await context.Contract.AddRangeAsync(contracts);
                await context.SaveChangesAsync();

                Console.WriteLine("✅ Seeded Contract data.");
            }
        }


        public static async Task SeedSalaries(DataContext context)
        {
            if (await context.Salary.AnyAsync()) return;

            var employees = await context.Employee.Include(e => e.Salaries).ToListAsync();
            if (employees == null || !employees.Any())
            {
                Console.WriteLine("⚠ Cannot seed Salaries because Employees are missing.");
                return;
            }

            var salaryData = await File.ReadAllTextAsync("Data/SalarySeedData.json");
            var salaries = JsonSerializer.Deserialize<List<Salary>>(salaryData);

            if (salaries != null)
            {
                foreach (var salary in salaries)
                {
                    var randomEmployee = employees[System.Security.Cryptography.RandomNumberGenerator.GetInt32(employees.Count)];
                    salary.EmployeeId = randomEmployee.EmployeeId;

                    randomEmployee.Salaries ??= new List<Salary>();
                    randomEmployee.Salaries.Add(salary);
                }

                await context.Salary.AddRangeAsync(salaries);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Seeded Salary data.");
        }
    }
}
