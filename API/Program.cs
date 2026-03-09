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

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // MongoDB: ignore unknown fields when deserializing POCOs
        ConventionRegistry.Register(
            "IgnoreExtraElements",
            new ConventionPack { new IgnoreExtraElementsConvention(true) },
            _ => true);

        // Map _id to custom Id properties (tránh lỗi "Element '_id' does not match any field")
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

        // ----------------------- CORS ------------------------------
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                // Cho phép tất cả localhost ports để tương thích với Angular dev server
                policy.WithOrigins("http://localhost:4300", "http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // RẤT QUAN TRỌNG cho SignalR và JWT
            });
        });

        // ----------------------- CONTROLLERS ------------------------
        builder.Services.AddControllers()
            .AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.PropertyNamingPolicy = null;
                opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // Cho phép username/UserName từ FE
            });

        // ------------------- MONGO DB (NEW) ----------------
        // Đọc cấu hình MongoDB từ appsettings.json → "MongoSettings"
        builder.Services.Configure<MongoSettings>(
            builder.Configuration.GetSection("MongoSettings"));

        // Đăng ký MongoClient (singleton)
        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        // Đăng ký IMongoDatabase (singleton) để inject vào repository/service
        builder.Services.AddSingleton<IMongoDatabase>(sp =>
        {
            var mongoSettings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoSettings.DatabaseName);
        });

        // ID generator + bootstrap (indexes + seed)
        builder.Services.AddSingleton<IMongoIdGenerator, MongoIdGenerator>();
        builder.Services.AddHostedService<MongoBootstrapHostedService>();

        // ----------------------- AUTOMAPPER -------------------------
        builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // ----------------------- JWT AUTH ---------------------------
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["TokenKey"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true, // Validate token expiration
                    ClockSkew = TimeSpan.FromMinutes(5) // Cho phép clock skew 5 phút để tránh lỗi do thời gian server/client khác nhau
                };

                // Cấu hình cho SignalR - xử lý token từ query string hoặc access token
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        
                        // Lấy token từ query string cho SignalR
                        var accessToken = context.Request.Query["access_token"];
                        
                        // Nếu là request đến SignalR hub và có token trong query string
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/dashboard-hub"))
                        {
                            context.Token = accessToken;
                            logger.LogInformation("📡 SignalR token from query string");
                            return Task.CompletedTask;
                        }
                        
                        // Đối với HTTP requests, token sẽ được lấy tự động từ Authorization header
                        // Nhưng chúng ta vẫn log để debug
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
                            // Chỉ log warning cho protected routes
                            if (path.Value.Contains("/api/") && !path.Value.Contains("/login") && !path.Value.Contains("/register"))
                            {
                                logger.LogWarning($"⚠️ No Authorization header found for protected route: {path}");
                            }
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        var path = context.Request.Path;
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        
                        logger.LogError(context.Exception, "❌ JWT Authentication failed");
                        logger.LogWarning("Path: {Path}", path);
                        logger.LogWarning("Authorization header: {Header}", authHeader ?? "None");
                        
                        // Log chi tiết exception
                        if (context.Exception != null)
                        {
                            logger.LogError("Exception type: {Type}", context.Exception.GetType().Name);
                            logger.LogError("Exception message: {Message}", context.Exception.Message);
                            
                            // Nếu là SecurityTokenExpiredException, log thêm thông tin
                            if (context.Exception is Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException expiredEx)
                            {
                                logger.LogWarning("Token expired at: {Expired}", expiredEx.Expires);
                            }
                        }
                        
                        // KHÔNG set context.ErrorResult - để middleware xử lý tự nhiên
                        // Chỉ log để debug
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("⛔ Authentication challenge triggered - 401 Unauthorized");
                        logger.LogWarning("Path: {Path}", context.Request.Path);
                        
                        // Log chi tiết để debug
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        logger.LogWarning("Authorization header: {Header}", authHeader ?? "None");
                        
                        // KHÔNG handle response ở đây - để ASP.NET Core xử lý tự nhiên
                        // Chỉ log thông tin để debug
                        // context.HandleResponse() sẽ ngăn controller nhận được request
                        
                        return Task.CompletedTask;
                    }
                };
            });

        // ----------------------- REPOSITORIES & SERVICES ----------------
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<TokenService>();
        builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        builder.Services.AddScoped<IContractRepository, ContractRepository>();
        builder.Services.AddScoped<ISalaryRepository, SalaryRepository>();
        builder.Services.AddScoped<IContactRepository, ContactRepository>();
        builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
        builder.Services.AddScoped<IRecuimentRepository, RecuimentRepository>();
        builder.Services.AddScoped<ITimeKeepingRepository, TimeKeepingRepository>();
        builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();

        // ----------------------- SWAGGER -------------------------
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // ----------------------- SIGNALR --------------------------
        builder.Services.AddSignalR();

        // ----------------------- DASHBOARD SERVICE -----------------
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddHostedService<DashboardBackgroundService>();                    

        var app = builder.Build(); 

        // ----------------------- EXCEPTION MIDDLEWARE --------------
        app.UseMiddleware<ExceptionMiddleware>();

        // ----------------------- DEV SWAGGER ------------------------
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseStaticFiles();

        // Bắt đầu định tuyến
        app.UseRouting();

        // Đảm bảo CORS được gọi trước MapHub và MapControllers
        app.UseCors("AllowAngular");

        app.UseAuthentication();
        app.UseAuthorization();

        // ------------------ MAPPING CONTROLLERS & HUB -----------------
        // Sử dụng MapControllers và MapHub trực tiếp (minimal API approach)
        app.MapControllers();
        app.MapHub<DashboardHub>("/dashboard-hub");

        await app.RunAsync("http://localhost:5002");
    }
}
