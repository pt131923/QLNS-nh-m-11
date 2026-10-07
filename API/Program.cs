using API.Data;
using API.Middleware;
using AutoMapper;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API.Hubs;
using API.Services;
using System.Linq;
using System.Threading.Tasks;
using API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using API.Entities;
using OfficeOpenXml;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // === MongoDB: Configure conventions & class mappings ===
        ConventionRegistry.Register(
            "IgnoreExtraElements",
            new ConventionPack { new IgnoreExtraElementsConvention(true) },
            _ => true);

        // Map _id to custom Id properties
        if (!BsonClassMap.IsClassMapRegistered(typeof(AppDepartment)))
            BsonClassMap.RegisterClassMap<AppDepartment>(cm => { cm.MapIdProperty(c => c.DepartmentId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(Employee)))
            BsonClassMap.RegisterClassMap<Employee>(cm => { cm.MapIdProperty(c => c.EmployeeId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
            BsonClassMap.RegisterClassMap<User>(cm => { cm.MapIdProperty(c => c.UserId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(Contract)))
            BsonClassMap.RegisterClassMap<Contract>(cm => { cm.MapIdProperty(c => c.ContractId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(Salary)))
            BsonClassMap.RegisterClassMap<Salary>(cm => { cm.MapIdProperty(c => c.SalaryId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(Leave)))
            BsonClassMap.RegisterClassMap<Leave>(cm => { cm.MapIdProperty(c => c.LeaveId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(Contact)))
            BsonClassMap.RegisterClassMap<Contact>(cm => { cm.MapIdProperty(c => c.ContactId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(TimeKeeping)))
            BsonClassMap.RegisterClassMap<TimeKeeping>(cm => { cm.MapIdProperty(c => c.TimeKeepingId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(Training)))
            BsonClassMap.RegisterClassMap<Training>(cm => { cm.MapIdProperty(c => c.TrainingId); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(Recuiment)))
            BsonClassMap.RegisterClassMap<Recuiment>(cm => { cm.MapIdProperty(c => c.Id); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(FileHistory)))
            BsonClassMap.RegisterClassMap<FileHistory>(cm => { cm.MapIdProperty(c => c.Id); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(ContactHistory)))
            BsonClassMap.RegisterClassMap<ContactHistory>(cm => { cm.MapIdProperty(c => c.Id); cm.AutoMap(); });
        if (!BsonClassMap.IsClassMapRegistered(typeof(Benefits)))
            BsonClassMap.RegisterClassMap<Benefits>(cm => { cm.MapIdProperty(c => c.Id); cm.AutoMap(); });

        // === EPPlus License ===
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // === CORS Policy ===
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                policy.WithOrigins("http://localhost:4300", "http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // === Controllers ===
        builder.Services.AddControllers()
            .AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.PropertyNamingPolicy = null;
                opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        // === MongoDB Configuration ===
        builder.Services.Configure<MongoSettings>(
            builder.Configuration.GetSection("MongoSettings"));

        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        builder.Services.AddSingleton<IMongoDatabase>(sp =>
        {
            var mongoSettings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoSettings.DatabaseName);
        });

        // === ✅ ĐĂNG KÝ ĐẦY ĐỦ TẤT CẢ COLLECTION ===
        builder.Services.AddScoped<IMongoCollection<Employee>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<Employee>("Employees");
        });

        builder.Services.AddScoped<IMongoCollection<AppDepartment>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<AppDepartment>("Departments");
        });

        builder.Services.AddScoped<IMongoCollection<FileHistory>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<FileHistory>("FileHistory");
        });

        builder.Services.AddScoped<IMongoCollection<Contract>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<Contract>("Contracts");
        });

        builder.Services.AddScoped<IMongoCollection<Salary>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<Salary>("Salaries");
        });

        builder.Services.AddScoped<IMongoCollection<Leave>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<Leave>("Leaves");
        });

        builder.Services.AddScoped<IMongoCollection<User>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<User>("Users");
        });

        builder.Services.AddScoped<IMongoCollection<Training>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<Training>("Trainings");
        });

        builder.Services.AddScoped<IMongoCollection<Recuiment>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<Recuiment>("Recuiments");
        });

        builder.Services.AddScoped<IMongoCollection<Contact>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<Contact>("Contacts");
        });

        builder.Services.AddScoped<IMongoCollection<TimeKeeping>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<TimeKeeping>("TimeKeepings");
        });

        // === Helpers & Background Services ===
        builder.Services.AddSingleton<IMongoIdGenerator, MongoIdGenerator>();
        builder.Services.AddHostedService<MongoBootstrapHostedService>();

        // === AutoMapper ===
        builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // === JWT Authentication ===
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["TokenKey"]!)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/dashboard-hub"))
                        {
                            context.Token = accessToken;
                            logger.LogInformation("📡 SignalR token from query string");
                            return Task.CompletedTask;
                        }
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader))
                        {
                            if (authHeader.StartsWith("Bearer "))
                            {
                                var token = authHeader.Substring("Bearer ".Length).Trim();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    context.Token = token;
                                    logger.LogInformation($"✅ Token extracted from Authorization header for {path} (Length: {token.Length})");
                                }
                            }
                            else
                            {
                                logger.LogWarning($"⚠️ Authorization header doesn't start with 'Bearer ' for {path}");
                            }
                        }
                        else
                        {
                            if (path.Value.Contains("/api/") && !path.Value.Contains("/login") && !path.Value.Contains("/register"))
                            {
                                logger.LogWarning($"⚠️ No Authorization header found for protected route: {path}");
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        var path = context.Request.Path;
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        logger.LogError(context.Exception, "❌ JWT Authentication failed");
                        logger.LogWarning("Path: {Path}", path);
                        logger.LogWarning("Authorization header: {Header}", authHeader ?? "None");
                        if (context.Exception != null)
                        {
                            logger.LogError("Exception type: {Type}", context.Exception.GetType().Name);
                            logger.LogError("Exception message: {Message}", context.Exception.Message);
                            if (context.Exception is SecurityTokenExpiredException expiredEx)
                            {
                                logger.LogWarning("Token expired at: {Expired}", expiredEx.Expires);
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("⛔ Authentication challenge triggered - 401 Unauthorized");
                        logger.LogWarning("Path: {Path}", context.Request.Path);
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        logger.LogWarning("Authorization header: {Header}", authHeader ?? "None");
                        return Task.CompletedTask;
                    }
                };
            });

        // === Repositories & Services ===
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<TokenService>();
        builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        builder.Services.AddScoped<IContractRepository, ContractRepository>();
        builder.Services.AddScoped<ISalaryRepository, SalaryRepository>();
        builder.Services.AddScoped<IPayrollService, PayrollService>();
        builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
        builder.Services.AddScoped<IRecuimentRepository, RecuimentRepository>();
        builder.Services.AddScoped<ITimeKeepingRepository, TimeKeepingRepository>();
        builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
        builder.Services.AddScoped<IContactRepository, ContactRepository>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();

        // === Swagger ===
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // === SignalR ===
        builder.Services.AddSignalR();

        // === Dashboard Service ===
        builder.Services.AddMemoryCache();
        builder.Services.AddHostedService<DashboardBackgroundService>();

        var app = builder.Build();

        // === Middleware Pipeline ===
        app.UseMiddleware<ExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("AllowAngular");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<DashboardHub>("/dashboard-hub");

        await app.RunAsync("http://localhost:5002");
    }
}
