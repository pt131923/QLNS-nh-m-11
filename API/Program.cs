using API.Data;
using API.Middleware;
using AutoMapper;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;
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

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ----------------------- CORS ------------------------------
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                // Cho phép tất cả localhost ports để tương thích với Angular dev server
                policy.WithOrigins("http://localhost:4300")
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
            });

        // ------------------- DB CONTEXT (FIX LỖI 500) ----------------
        builder.Services.AddDbContextFactory<DataContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
                    ClockSkew = TimeSpan.Zero // Không cho phép clock skew để tránh token hết hạn sớm
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
                        
                        // Nếu không có token trong query string, thử lấy từ Authorization header
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                            {
                                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                                logger.LogInformation($"✅ Token extracted from Authorization header for {path}");
                            }
                            else
                            {
                                logger.LogWarning($"⚠️ No Authorization header found for {path}");
                            }
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "❌ JWT Authentication failed");
                        
                        // Log thêm thông tin để debug
                        logger.LogWarning("Token: {Token}", context.Request.Headers["Authorization"].FirstOrDefault());
                        logger.LogWarning("Path: {Path}", context.Request.Path);
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("⛔ Authentication challenge triggered - 401 Unauthorized");
                        logger.LogWarning("Path: {Path}", context.Request.Path);
                        
                        // Đảm bảo response là 401 với message rõ ràng
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        
                        var errorResponse = new
                        {
                            statusCode = 401,
                            message = "Unauthorized - Invalid or expired token",
                            path = context.Request.Path
                        };
                        
                        return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
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


        // ----------------------- MIGRATION + SEED -------------------
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                // Sử dụng factory để lấy context cho Migration/Seeding
                // (Vì DataContext không còn là Scoped service nữa)
                var contextFactory = services.GetRequiredService<IDbContextFactory<DataContext>>();
                using var context = contextFactory.CreateDbContext();

                await context.Database.MigrateAsync();

                await Seed.SeedDepartments(context);
                await Seed.SeedEmployees(context);
                await Seed.SeedContracts(context);
                await Seed.SeedSalaries(context);
                await Seed.SeedTraining(context);
                await Seed.SeedRecuiment(context);
                await Seed.SeedUsers(context);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "❌ Migration hoặc Seed thất bại");
            }
        }

        await app.RunAsync("http://localhost:5002");
    }
}