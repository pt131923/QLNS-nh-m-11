using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    // Sử dụng primary constructor cho DataContext
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        // ====================================================================
        // EXISTING DbSet PROPERTIES
        // ====================================================================
        public DbSet<Employee> Employee { get; set; }
        public DbSet<AppDepartment> Department { get; set; }
        public DbSet<Contract> Contract { get; set; }
        public DbSet<Salary> Salary { get; set; }
        public DbSet<TimeKeeping> TimeKeeping { get; set; }
        public DbSet<Contact> Contact { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<FileHistory> FileHistory { get; set; }

        // ====================================================================
        // ADDED DbSet PROPERTIES (To fix CS1061 errors and support all dashboard cards)
        // ====================================================================
        // Entities for Dashboard Cards (Documents, Overtime, Training, Leaves, etc.)
        public DbSet<HrDocument> HrDocument { get; set; }
        public DbSet<WorkLog> WorkLog { get; set; }
        public DbSet<Training> Training { get; set; }
        public DbSet<LeaveRequest> LeaveRequest { get; set; }
        public DbSet<Recuiment> Recuiment { get; set; }
        public DbSet<Benefits> Benefits { get; set; }
        public DbSet<SystemSetting> SystemSetting { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ====================================================================
            // EXISTING CONFIGURATION
            // ====================================================================

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
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Entity<Salary>()
                .Property(s => s.Date)
                .IsRequired();

            builder.Entity<Salary>()
                .HasOne(s => s.Employee)
                .WithMany(e => e.Salaries)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // TimeKeeping configuration
            builder.Entity<TimeKeeping>()
                .HasKey(t => t.TimeKeepingId)
                .HasName("TimeKeepingId");

            builder.Entity<TimeKeeping>()
                .Property(t => t.TimeKeepingId)
                .ValueGeneratedOnAdd();

            builder.Entity<TimeKeeping>()
                .Property(t => t.Date)
                .IsRequired();

            builder.Entity<TimeKeeping>()
                .HasOne(t => t.Employee)
                .WithMany(e => e.TimeKeeping)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 🛠️ SỬA LỖI: Contact configuration (Đã gộp và sửa code bị trùng lặp ở cuối file)
            builder.Entity<Contact>()
                .HasKey(c => c.ContactId);

            // User configuration (Nếu cần, nên thêm HasKey ở đây)
            builder.Entity<User>()
                .HasKey(u => u.UserId); // Giả định khóa chính là Id

            // FileHistory configuration (Nếu cần, nên thêm HasKey ở đây)
            builder.Entity<FileHistory>()
                .HasKey(f => f.Id); // Giả định khóa chính là Id

            
            // ====================================================================
            // ADDED BASIC CONFIGURATION FOR NEW ENTITIES 
            // ====================================================================
            
            // HrDocument configuration
            builder.Entity<HrDocument>()
                .HasKey(d => d.Id); // Giả định khóa chính là Id

            // WorkLog configuration
            builder.Entity<WorkLog>()
                .HasKey(w => w.WorkLogId);
            
            // Training configuration
            builder.Entity<Training>()
                .HasKey(t => t.TrainingId);

            builder.Entity<Training>()
                .Property(t => t.Cost)
                .HasColumnType("decimal(18,2)");

            // LeaveRequest configuration
            builder.Entity<LeaveRequest>()
                .HasKey(l => l.LeaveRequestId);

            builder.Entity<LeaveRequest>()
                .Property(l => l.Days)
                .HasColumnType("decimal(5,1)");

            // Recuiment configuration
            builder.Entity<Recuiment>()
                .HasKey(r => r.Id);

            builder.Entity<Recuiment>()
                .Property(r => r.Amount)
                .HasColumnType("decimal(18,2)");

            // Benefits configuration
            builder.Entity<Benefits>()
                .HasKey(b => b.Id);

            builder.Entity<Benefits>()
                .Property(b => b.Cost)
                .HasColumnType("decimal(18,2)");

            // Salary additional decimal configurations
            builder.Entity<Salary>()
                .Property(s => s.MonthlySalary)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Salary>()
                .Property(s => s.Bonus)
                .HasColumnType("decimal(18,2)");

            // TimeKeeping decimal configuration
            builder.Entity<TimeKeeping>()
                .Property(t => t.TotalHours)
                .HasColumnType("decimal(5,2)");

            // SystemSetting configuration
            builder.Entity<SystemSetting>()
                .HasKey(s => s.Id); // Giả định khóa chính là Id

        }
    }
}