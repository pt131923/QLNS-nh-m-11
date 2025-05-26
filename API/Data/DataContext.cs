using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options) { }

        public DbSet<Employee> Employee { get; set; }
        public DbSet<AppDepartment> Department { get; set; }
        public DbSet<Contract> Contract { get; set; }
        public DbSet<Salary> Salary { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // AppDepartment configuration
            builder.Entity<AppDepartment>()
                .HasKey(d => d.DepartmentId)
                .HasName("DepartmentId");

            builder.Entity<AppDepartment>()
                .Property(d => d.DepartmentId)
                .ValueGeneratedOnAdd();

            builder.Entity<AppDepartment>()
                .Property(d => d.Name)
                .IsRequired();

            builder.Entity<AppDepartment>()
                .HasMany(d => d.Employee)
                .WithOne(e => e.Department)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee configuration
            builder.Entity<Employee>()
                .HasKey(e => e.EmployeeId)
                .HasName("EmployeeId");

            builder.Entity<Employee>()
                .Property(e => e.EmployeeId)
                .ValueGeneratedOnAdd();

            builder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employee)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Contract configuration
            builder.Entity<Contract>()
                .HasKey(c => c.ContractId)
                .HasName("ContractId");

            builder.Entity<Contract>()
                .Property(c => c.ContractId)
                .ValueGeneratedOnAdd();

            // Salary configuration
            builder.Entity<Salary>()
                .HasKey(s => s.SalaryId)
                .HasName("SalaryId");

            builder.Entity<Salary>()
                .Property(s => s.SalaryId)
                .ValueGeneratedOnAdd();

            builder.Entity<Salary>()
                .Property(s => s.TotalSalary)
                .IsRequired();

            builder.Entity<Salary>()
                .Property(s => s.Date)
                .IsRequired();

            builder.Entity<Salary>()
                .HasOne(s => s.Employee)
                .WithMany(e => e.Salaries)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
