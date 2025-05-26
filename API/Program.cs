using API.Data;
using API.Middleware;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using API.Helpers;
using API.Interfaces;
using API.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        // Controllers + JSON
        builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
        // Remove the following lines to prevent camelCase conversion
        // .AddJsonOptions(options =>
        // {
        //     options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // })
        // .ConfigureApiBehaviorOptions(options =>
        // {
        //     options.SuppressMapClientErrors = true; // You might want to keep this depending on desired API behavior
        // });
        ; // Keep this semicolon after AddControllers()

        // AutoMapper (Ensure this is configured correctly, the two AddAutoMapper calls might be redundant)
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new AutoMapperProfiles());
        });
        builder.Services.AddSingleton(mapperConfig.CreateMapper());
        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies()); // This might be enough on its own

        // Services
        builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        builder.Services.AddScoped<IContractRepository, ContractRepository>();
        builder.Services.AddScoped<ISalaryRepository, SalaryRepository>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // DbContext
        builder.Services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // App
        var app = builder.Build();

        // Middleware
        // Keep UseDeveloperExceptionPage() in development for detailed error information
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage(); // Good for development debugging

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
                c.RoutePrefix = "swagger";
            });
        }
        // else
        // {
        //     // You might want a different error handling mechanism in production
        //     // app.UseExceptionHandler("/Error");
        //     // app.UseHsts(); // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        // }


        // app.UseMiddleware<ExceptionMiddleware>(); // Use custom middleware for consistent error handling across environments
        app.UseMiddleware<ExceptionMiddleware>(); // Consider placing this early in the pipeline

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("AllowLocalhost4300");

        // Authentication and Authorization should typically come after UseRouting and UseCors
        app.UseAuthentication(); // If you add authentication later
        app.UseAuthorization(); // If you add authorization later

        app.MapControllers();

        // Remove or move this line, as it's typically placed inside the development check
        // app.UseDeveloperExceptionPage(); // This is already inside the dev check block above


        // Database migrate
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<DataContext>();
                await context.Database.MigrateAsync();
                // Ensure Seed methods are safe to run multiple times or have checks
                await Seed.SeedDepartments(context);
                await Seed.SeedEmployees(context);
                await Seed.SeedContracts(context);
                await Seed.SeedSalaries(context);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during migration or seeding");
            }
        }

        app.Run();
    }
}