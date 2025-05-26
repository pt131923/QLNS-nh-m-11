using System.Text.Json;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedDepartments(DataContext context)
        {
            if (await context.Department.AnyAsync())
                return;

            var departmentData = await File.ReadAllTextAsync("Data/DepartmentSeedData.json");
            var departments = JsonSerializer.Deserialize<List<AppDepartment>>(departmentData);

            if (departments != null)
            {
                await context.Department.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Seeded Department data.");
        }

        public static async Task SeedDepartmentsWithEmployees(DataContext context)
        {
            if (await context.Department.AnyAsync())
                return;

            var departmentData = await File.ReadAllTextAsync("Data/DepartmentSeedDataWithEmployees.json");
            var departments = JsonSerializer.Deserialize<List<AppDepartment>>(departmentData);

            if (departments != null)
            {
                await context.Department.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Seeded Department data with Employees.");
        }

        public static async Task SeedEmployees(DataContext context)
        {
            if (await context.Employee.AnyAsync())
                return;

            // Đảm bảo Department đã có trong DB
            var departments = await context.Department.ToListAsync();
            if (departments == null || !departments.Any())
            {
                Console.WriteLine("⚠ Cannot seed Employees because Departments are missing.");
                return;
            }

            var employeeData = await File.ReadAllTextAsync("Data/EmployeeSeedData.json");
            var employees = JsonSerializer.Deserialize<List<Employee>>(employeeData);

            if (employees != null)
            {
                foreach (var employee in employees)
                {
                    // Chọn department ngẫu nhiên sử dụng RandomNumberGenerator
                    int randomIndex = System.Security.Cryptography.RandomNumberGenerator.GetInt32(departments.Count);
                    var randomDepartment = departments[randomIndex];
                    employee.DepartmentId = randomDepartment.DepartmentId;
                }

                await context.Employee.AddRangeAsync(employees);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Seeded Employee data.");
        }

        public static async Task SeedContracts(DataContext context)
        {
            if (await context.Contract.AnyAsync())
                return;

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
                    // Chọn employee ngẫu nhiên sử dụng RandomNumberGenerator
                    int randomIndex = System.Security.Cryptography.RandomNumberGenerator.GetInt32(employees.Count);
                    var randomEmployee = employees[randomIndex];
                    contract.EmployeeId = randomEmployee.EmployeeId;

                    // Thêm contract vào danh sách contracts của employee
                    if (randomEmployee.Contracts == null)
                    {
                        randomEmployee.Contracts = new List<Contract>();
                    }
                    randomEmployee.Contracts.Add(contract);
                }

                await context.Contract.AddRangeAsync(contracts);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Seeded Contract data.");
        }
        
        public static async Task SeedSalaries(DataContext context)
        {
            if (await context.Salary.AnyAsync())
                return;

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
                    // Chọn employee ngẫu nhiên sử dụng RandomNumberGenerator
                    int randomIndex = System.Security.Cryptography.RandomNumberGenerator.GetInt32(employees.Count);
                    var randomEmployee = employees[randomIndex];
                    salary.EmployeeId = randomEmployee.EmployeeId;

                    // Thêm salary vào danh sách salaries của employee
                    if (randomEmployee.Salaries == null)
                    {
                        randomEmployee.Salaries = new List<Salary>();
                    }
                    randomEmployee.Salaries.Add(salary);
                }

                await context.Salary.AddRangeAsync(salaries);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Seeded Salary data.");
        }
    }
}