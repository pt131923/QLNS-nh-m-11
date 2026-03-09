using System.Text.Json;
using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace API.Services
{
    /// <summary>
    /// Tạo index và seed dữ liệu khởi tạo vào MongoDB (thay thế EF migrations + Seed).
    /// </summary>
    public class MongoBootstrapHostedService : IHostedService
    {
        private readonly IMongoDatabase _db;
        private readonly IMongoIdGenerator _idGenerator;
        private readonly ILogger<MongoBootstrapHostedService> _logger;

        public MongoBootstrapHostedService(
            IMongoDatabase db,
            IMongoIdGenerator idGenerator,
            ILogger<MongoBootstrapHostedService> logger)
        {
            _db = db;
            _idGenerator = idGenerator;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await EnsureIndexesAsync(cancellationToken);
            await SeedWithSchemaRecoveryAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task EnsureIndexesAsync(CancellationToken ct)
        {
            _logger.LogInformation("🔧 Ensuring MongoDB indexes...");

            var users = _db.GetCollection<User>("Users");
            await users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(x => x.UserName),
                    new CreateIndexOptions { Unique = true, Name = "UX_User_UserName" }),
                cancellationToken: ct);

            var departments = _db.GetCollection<AppDepartment>("Departments");
            await departments.Indexes.CreateOneAsync(
                new CreateIndexModel<AppDepartment>(
                    Builders<AppDepartment>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions { Unique = true, Name = "UX_Department_Name" }),
                cancellationToken: ct);

            var employees = _db.GetCollection<Employee>("Employees");
            await employees.Indexes.CreateOneAsync(
                new CreateIndexModel<Employee>(
                    Builders<Employee>.IndexKeys.Ascending(x => x.EmployeeName),
                    new CreateIndexOptions { Unique = true, Name = "UX_Employee_EmployeeName" }),
                cancellationToken: ct);

            var fileHistory = _db.GetCollection<FileHistory>("FileHistory");
            await fileHistory.Indexes.CreateOneAsync(
                new CreateIndexModel<FileHistory>(
                    Builders<FileHistory>.IndexKeys.Ascending(x => x.FileHash),
                    new CreateIndexOptions { Unique = true, Name = "UX_FileHistory_FileHash" }),
                cancellationToken: ct);

            _logger.LogInformation("✅ MongoDB indexes ensured.");
        }

        private async Task SeedWithSchemaRecoveryAsync(CancellationToken ct)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    await SeedAsync(ct);
                    return;
                }
                catch (System.FormatException ex)
                {
                    _logger.LogWarning(ex, "⚠️ Lỗi schema MongoDB (_id). Đang xóa collections và seed lại...");
                    await DropSeededCollectionsAsync(ct);
                    if (attempt == 1) throw;
                }
            }
        }

        private async Task DropSeededCollectionsAsync(CancellationToken ct)
        {
            var names = new[] { "Departments", "Employees", "Users", "Trainings", "Recuiments", "Contracts", "Salaries", "Counters" };
            foreach (var name in names)
            {
                try { await _db.DropCollectionAsync(name, ct); } catch { /* ignore if not exists */ }
            }
            _logger.LogInformation("✅ Đã xóa các collection cũ.");
        }

        private async Task SeedAsync(CancellationToken ct)
        {
            var departmentCol = _db.GetCollection<AppDepartment>("Departments");
            long deptCount = await departmentCol.CountDocumentsAsync(_ => true, cancellationToken: ct);
            if (deptCount > 0)
            {
                try
                {
                    _ = await departmentCol.Find(_ => true).Limit(1).FirstOrDefaultAsync(ct);
                }
                catch (System.FormatException)
                {
                    await DropSeededCollectionsAsync(ct);
                    throw;
                }
            }

            // Departments
            if (deptCount == 0)
            {
                var departmentData = await File.ReadAllTextAsync("Data/DepartmentSeedData.json", ct);
                var departments = JsonSerializer.Deserialize<List<AppDepartment>>(departmentData);
                if (departments != null)
                {
                    foreach (var d in departments)
                    {
                        if (d.DepartmentId == 0)
                            d.DepartmentId = await _idGenerator.NextAsync("Departments", ct);
                    }

                    await departmentCol.InsertManyAsync(departments, cancellationToken: ct);
                    _logger.LogInformation("✅ Seeded Department data.");
                }
            }

            // Employees
            var employeeCol = _db.GetCollection<Employee>("Employees");
            if (await employeeCol.CountDocumentsAsync(_ => true, cancellationToken: ct) == 0)
            {
                var employeeData = await File.ReadAllTextAsync("Data/EmployeeSeedData.json", ct);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var employeeDtos = JsonSerializer.Deserialize<List<EmployeeSeedDto>>(employeeData, options);

                if (employeeDtos != null && employeeDtos.Any())
                {
                    var deptList = await departmentCol.Find(_ => true).ToListAsync(ct);
                    var deptMap = deptList.ToDictionary(d => d.Name, d => d.DepartmentId);

                    var employees = new List<Employee>();
                    foreach (var dto in employeeDtos)
                    {
                        if (dto.Department == null || string.IsNullOrWhiteSpace(dto.Department.Name)) continue;
                        if (!deptMap.TryGetValue(dto.Department.Name, out var deptId)) continue;

                        var emp = new Employee
                        {
                            EmployeeId = await _idGenerator.NextAsync("Employees", ct),
                            EmployeeName = dto.EmployeeName,
                            EmployeeEmail = dto.EmployeeEmail,
                            EmployeePhone = dto.EmployeePhone,
                            EmployeeAddress = dto.EmployeeAddress,
                            DepartmentId = deptId,
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
                            Specialization = dto.Specialization,
                            CreatedAt = DateTime.UtcNow
                        };
                        employees.Add(emp);
                    }

                    if (employees.Any())
                    {
                        await employeeCol.InsertManyAsync(employees, cancellationToken: ct);
                        _logger.LogInformation("✅ Seeded Employee data.");
                    }
                }
            }

            // Users
            var userCol = _db.GetCollection<User>("Users");
            if (await userCol.CountDocumentsAsync(_ => true, cancellationToken: ct) == 0)
            {
                using var hmac = new System.Security.Cryptography.HMACSHA512();
                var passwordSalt = hmac.Key;
                var passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes("admin123"));

                var users = new List<User>
                {
                    new User
                    {
                        UserId = await _idGenerator.NextAsync("Users", ct),
                        UserName = "admin",
                        Email = "admin@qlns.com",
                        PhoneNumber = "0123456789",
                        Address = "123 Admin Street",
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Role = "admin",
                        Image = "admin-avatar.png"
                    },
                    new User
                    {
                        UserId = await _idGenerator.NextAsync("Users", ct),
                        UserName = "manager",
                        Email = "manager@qlns.com",
                        PhoneNumber = "0987654321",
                        Address = "456 Manager Avenue",
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Role = "manager",
                        Image = "manager-avatar.png"
                    },
                    new User
                    {
                        UserId = await _idGenerator.NextAsync("Users", ct),
                        UserName = "employee",
                        Email = "employee@qlns.com",
                        PhoneNumber = "0555666777",
                        Address = "789 Employee Road",
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Role = "user",
                        Image = "employee-avatar.png"
                    }
                };

                await userCol.InsertManyAsync(users, cancellationToken: ct);
                _logger.LogInformation("✅ Seeded User accounts: admin/manager/employee (password: admin123)");
            }

            // Training
            var trainingCol = _db.GetCollection<Training>("Trainings");
            if (await trainingCol.CountDocumentsAsync(_ => true, cancellationToken: ct) == 0)
            {
                var trainings = new List<Training>
                {
                    new Training { TrainingId = await _idGenerator.NextAsync("Trainings", ct), Title = "React Advanced", Description = "Advanced React Development", Date = DateTime.Now.AddDays(30), Cost = 5000000 },
                    new Training { TrainingId = await _idGenerator.NextAsync("Trainings", ct), Title = "C# .NET Core", Description = "Backend Development with .NET", Date = DateTime.Now.AddDays(60), Cost = 7000000 },
                    new Training { TrainingId = await _idGenerator.NextAsync("Trainings", ct), Title = "SQL Database", Description = "Database Design and Optimization", Date = DateTime.Now.AddDays(90), Cost = 3000000 },
                    new Training { TrainingId = await _idGenerator.NextAsync("Trainings", ct), Title = "Agile Methodology", Description = "Project Management with Agile", Date = DateTime.Now.AddDays(45), Cost = 2000000 },
                    new Training { TrainingId = await _idGenerator.NextAsync("Trainings", ct), Title = "DevOps & CI/CD", Description = "DevOps practices and CI/CD pipelines", Date = DateTime.Now.AddDays(75), Cost = 6000000 },
                    new Training { TrainingId = await _idGenerator.NextAsync("Trainings", ct), Title = "Cloud Computing", Description = "AWS/Azure cloud architecture", Date = DateTime.Now.AddDays(120), Cost = 8000000 }
                };

                await trainingCol.InsertManyAsync(trainings, cancellationToken: ct);
                _logger.LogInformation("✅ Seeded Training data.");
            }

            // Recuiment
            var recuimentCol = _db.GetCollection<Recuiment>("Recuiments");
            if (await recuimentCol.CountDocumentsAsync(_ => true, cancellationToken: ct) == 0)
            {
                var recuiments = new List<Recuiment>
                {
                    new Recuiment { Id = await _idGenerator.NextAsync("Recuiments", ct), CustomerName = "Tech Solutions", Amount = 15000000, OpeningsCount = 5 },
                    new Recuiment { Id = await _idGenerator.NextAsync("Recuiments", ct), CustomerName = "Global Corp", Amount = 25000000, OpeningsCount = 8 },
                    new Recuiment { Id = await _idGenerator.NextAsync("Recuiments", ct), CustomerName = "Startup Inc", Amount = 8000000, OpeningsCount = 3 },
                    new Recuiment { Id = await _idGenerator.NextAsync("Recuiments", ct), CustomerName = "Enterprise Ltd", Amount = 35000000, OpeningsCount = 12 }
                };

                await recuimentCol.InsertManyAsync(recuiments, cancellationToken: ct);
                _logger.LogInformation("✅ Seeded Recuiment data.");
            }

            // Contracts (cần có Employees trước)
            var contractCol = _db.GetCollection<Contract>("Contracts");
            if (await contractCol.CountDocumentsAsync(_ => true, cancellationToken: ct) == 0)
            {
                var empList = await employeeCol.Find(_ => true).Limit(5).ToListAsync(ct);
                if (empList.Any())
                {
                    var contracts = new List<Contract>();
                    var now = DateTime.UtcNow;
                    foreach (var emp in empList)
                    {
                        contracts.Add(new Contract
                        {
                            ContractId = await _idGenerator.NextAsync("Contracts", ct),
                            ContractName = $"HĐLĐ-{emp.EmployeeName}",
                            EmployeeName = emp.EmployeeName,
                            ContractType = "Chính thức",
                            BasicSalary = 15000000,
                            Allowance = 2000000,
                            CreateAt = now,
                            UpdateAt = now,
                            JobDescription = "Nhân viên",
                            ContractTerm = "12 tháng",
                            StartDate = now.AddMonths(-6),
                            EndDate = now.AddMonths(6),
                            WorkLocation = "Hà Nội",
                            Leaveofabsence = "12 ngày/năm",
                            Status = "Đang hiệu lực"
                        });
                    }
                    await contractCol.InsertManyAsync(contracts, cancellationToken: ct);
                    _logger.LogInformation("✅ Seeded Contract data.");
                }
            }

            // Salaries (cần có Employees trước)
            var salaryCol = _db.GetCollection<Salary>("Salaries");
            if (await salaryCol.CountDocumentsAsync(_ => true, cancellationToken: ct) == 0)
            {
                var empList2 = await employeeCol.Find(_ => true).Limit(5).ToListAsync(ct);
                if (empList2.Any())
                {
                    var salaries = new List<Salary>();
                    var now = DateTime.UtcNow;
                    foreach (var emp in empList2)
                    {
                        var monthly = 15000000m;
                        var bonus = 2000000m;
                        salaries.Add(new Salary
                        {
                            SalaryId = await _idGenerator.NextAsync("Salaries", ct),
                            EmployeeId = emp.EmployeeId,
                            EmployeeName = emp.EmployeeName,
                            MonthlySalary = monthly,
                            Bonus = bonus,
                            TotalSalary = monthly + bonus,
                            SalaryNotes = "Lương tháng",
                            Date = now,
                            Amount = (long)(monthly + bonus)
                        });
                    }
                    await salaryCol.InsertManyAsync(salaries, cancellationToken: ct);
                    _logger.LogInformation("✅ Seeded Salary data.");
                }
            }
        }
    }
}

