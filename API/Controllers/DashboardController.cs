using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using API.Entities;
using System.Collections.Generic;
using System.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(
        IDashboardService _dashboardService,
        IMongoDatabase _db) : ControllerBase
    {
        public record RecentEmployeeDto(int EmployeeId, string EmployeeName, string DepartmentName);
        public record UpcomingItemDto(string Type, string Title, string Subtitle, DateTime Date, int DaysLeft, string Link);
        public record DashboardFullResponse(
            DashboardSummary Summary,
            List<RecentEmployeeDto> RecentEmployees,
            List<UpcomingItemDto> Upcoming
        );

        // --- API ENDPOINTS ---

        [Authorize]
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                // Log để debug
                var username = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var userId = User?.FindFirst("userId")?.Value;
                var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
                
                Console.WriteLine($"📡 GetSummary called - Authenticated: {isAuthenticated}, Username: {username}, UserId: {userId}");
                
                // Kiểm tra xem user có được authenticate không
                if (!isAuthenticated)
                {
                    Console.WriteLine("❌ User is not authenticated in GetSummary");
                    return Unauthorized(new { message = "User is not authenticated", statusCode = 401 });
                }

                Console.WriteLine("✅ User authenticated, fetching summary...");
                var summary = await _dashboardService.GetDashboardSummaryAsync();
                Console.WriteLine("✅ Summary fetched successfully");
                return Ok(summary);
            }
            catch (Exception ex)
            {
                // Log lỗi và trả về error response
                Console.WriteLine($"❌ Error in GetSummary: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new 
                { 
                    message = "An error occurred while fetching dashboard summary",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }

        [Authorize]
        [HttpGet("full")]
        public async Task<IActionResult> GetFullDashboard()
        {
            try
            {
                var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
                if (!isAuthenticated)
                    return Unauthorized(new { message = "User is not authenticated", statusCode = 401 });

                // Summary
                var summary = await _dashboardService.GetDashboardSummaryAsync();

                // Recent employees (sort by EmployeeId desc as a proxy for "new")
                var employeesCol = _db.GetCollection<Employee>("Employees");
                var departmentsCol = _db.GetCollection<AppDepartment>("Departments");

                var recentEmployeesRaw = await employeesCol
                    .Find(_ => true)
                    .SortByDescending(e => e.EmployeeId)
                    .Limit(5)
                    .Project(e => new { e.EmployeeId, e.EmployeeName, e.DepartmentId })
                    .ToListAsync();

                var deptIds = recentEmployeesRaw.Select(x => x.DepartmentId).Distinct().ToList();
                var depts = await departmentsCol
                    .Find(d => deptIds.Contains(d.DepartmentId))
                    .Project(d => new { d.DepartmentId, d.Name })
                    .ToListAsync();
                var deptMap = depts.ToDictionary(d => d.DepartmentId, d => d.Name);

                var recentEmployees = recentEmployeesRaw
                    .Select(e => new RecentEmployeeDto(
                        EmployeeId: e.EmployeeId,
                        EmployeeName: e.EmployeeName,
                        DepartmentName: deptMap.TryGetValue(e.DepartmentId, out var name) ? name : "—"
                    ))
                    .ToList();

                // Upcoming items
                var upcoming = new List<UpcomingItemDto>();
                var today = DateTime.Today;

                var contractsCol = _db.GetCollection<Contract>("Contracts");
                var trainingsCol = _db.GetCollection<Training>("Trainings");
                var leavesCol = _db.GetCollection<Leave>("Leaves");

                var expiringContracts = await contractsCol
                    .Find(c => c.EndDate >= today && c.EndDate <= today.AddDays(30))
                    .SortBy(c => c.EndDate)
                    .Limit(3)
                    .Project(c => new { c.ContractName, c.EmployeeName, c.EndDate })
                    .ToListAsync();

                foreach (var c in expiringContracts)
                {
                    var daysLeft = (c.EndDate.Date - today).Days;
                    upcoming.Add(new UpcomingItemDto(
                        Type: "contract",
                        Title: c.ContractName ?? "Contract",
                        Subtitle: string.IsNullOrEmpty(c.EmployeeName) ? "Expiring soon" : c.EmployeeName,
                        Date: c.EndDate,
                        DaysLeft: daysLeft,
                        Link: "/contracts"
                    ));
                }

                var upcomingTrainings = await trainingsCol
                    .Find(t => t.Date >= today && t.Date <= today.AddDays(60))
                    .SortBy(t => t.Date)
                    .Limit(3)
                    .Project(t => new { t.Title, t.Description, t.Date })
                    .ToListAsync();

                foreach (var t in upcomingTrainings)
                {
                    var daysLeft = (t.Date.Date - today).Days;
                    upcoming.Add(new UpcomingItemDto(
                        Type: "training",
                        Title: t.Title ?? "Training",
                        Subtitle: string.IsNullOrEmpty(t.Description) ? "Scheduled" : t.Description,
                        Date: t.Date,
                        DaysLeft: daysLeft,
                        Link: "/trainings"
                    ));
                }

                var pendingLeaves = await leavesCol
                    .Find(l => l.Status == "Pending" || l.Status == "pending")
                    .SortBy(l => l.StartDate)
                    .Limit(3)
                    .Project(l => new { l.Reason, l.StartDate, l.UserId, l.Status })
                    .ToListAsync();

                foreach (var l in pendingLeaves)
                {
                    var daysLeft = (l.StartDate.Date - today).Days;
                    upcoming.Add(new UpcomingItemDto(
                        Type: "leave",
                        Title: string.IsNullOrEmpty(l.Reason) ? "Leave request" : l.Reason,
                        Subtitle: $"UserId: {l.UserId}",
                        Date: l.StartDate,
                        DaysLeft: daysLeft,
                        Link: "/leaves"
                    ));
                }

                upcoming = upcoming
                    .OrderBy(x => x.DaysLeft)
                    .ThenBy(x => x.Date)
                    .Take(6)
                    .ToList();

                return Ok(new DashboardFullResponse(
                    Summary: summary,
                    RecentEmployees: recentEmployees,
                    Upcoming: upcoming
                ));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetFullDashboard: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while fetching full dashboard", error = ex.Message });
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Dashboard API is working",
                time = DateTime.Now,
                authenticated = User.Identity?.IsAuthenticated ?? false
            });
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            Console.WriteLine("📡 Ping endpoint called");
            return Ok(new { message = "pong", timestamp = DateTime.Now });
        }

        [HttpPost("trigger-realtime-update")]
        public async Task<IActionResult> TriggerRealtimeUpdate()
        {
            await _dashboardService.TriggerRealtimeUpdateAsync();

            return Ok(new
            {
                status = "Real-time update triggered successfully",
                time = DateTime.Now
            });
        }

        [HttpPost("notify-data-changed")]
        public async Task<IActionResult> NotifyDataChanged()
        {
            await _dashboardService.NotifyDataChangedAsync();

            return Ok(new
            {
                status = "Data change notification sent successfully",
                time = DateTime.Now
            });
        }

        [Authorize]
        [HttpGet("debug-data")]
        public async Task<IActionResult> GetDebugData()
        {
            var trainings = _db.GetCollection<Training>("Trainings");
            var recuiments = _db.GetCollection<Recuiment>("Recuiments");
            var salaries = _db.GetCollection<Salary>("Salaries");

            var trainingList = await trainings.Find(_ => true).ToListAsync();
            var recuimentList = await recuiments.Find(_ => true).ToListAsync();
            var salaryList = await salaries.Find(_ => true).ToListAsync();

            return Ok(new
            {
                Training = new
                {
                    Count = trainingList.Count,
                    TotalCost = trainingList.Sum(t => t.Cost),
                    Data = trainingList.Select(t => new { t.TrainingId, t.Title, t.Cost })
                },
                Recuiment = new
                {
                    Count = recuimentList.Count,
                    TotalAmount = recuimentList.Sum(r => r.Amount),
                    Data = recuimentList.Select(r => new { r.Id, r.CustomerName, r.Amount, r.OpeningsCount })
                },
                Salary = new
                {
                    Count = salaryList.Count,
                    TotalAmount = salaryList.Sum(s => s.TotalSalary),
                    Data = salaryList.Select(s => new { s.SalaryId, s.EmployeeId, s.Amount, s.Date })
                }
            });
        }

        [Authorize]
        [HttpGet("current-user")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst("userId")?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            return Ok(new
            {
                userId = userId,
                username = username,
                displayName = username,
                role = User.FindFirst("role")?.Value ?? "user",
                avatarUrl = "/assets/default-avatar.png"
            });
        }
    }
}