using API.Data;
using API.Middleware;
using AutoMapper;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using API.Repositories;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // -----------------------------
        //          SERVICES
        // -----------------------------

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalhost4300", policy =>
            {
                policy.WithOrigins("http://localhost:4300")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Controllers + JSON giữ nguyên PascalCase
        builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = null;
        });

        // DbContext
        builder.Services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // AutoMapper
        builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // -----------------------------
        //          REPOSITORIES
        // -----------------------------

        // LƯU Ý QUAN TRỌNG:
        // Hãy đảm bảo tất cả Repository sau nằm trong đúng namespace API.Data
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        builder.Services.AddScoped<IContractRepository, ContractRepository>(); // FIX CS0246
        builder.Services.AddScoped<ISalaryRepository, SalaryRepository>();
        builder.Services.AddScoped<IContactRepository, ContactRepository>();

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // -----------------------------
        //          APPLICATION
        // -----------------------------
        var app = builder.Build();

        // Middleware Exception
        app.UseMiddleware<ExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("AllowLocalhost4300");

        // Auth
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // -----------------------------
        //      MIGRATION & SEED
        // -----------------------------
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<DataContext>();

                await context.Database.MigrateAsync();

                await Seed.SeedDepartments(context);
                await Seed.SeedEmployees(context);
                await Seed.SeedContracts(context);
                await Seed.SeedSalaries(context);

                // Nếu có seed user thì mở dòng này:
                // await Seed.SeedUsers(context);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "❌ Migration hoặc Seed thất bại");
            }
        }

        app.Run();
    }
}
