using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(
        IDashboardService _dashboardService,
        IDbContextFactory<DataContext> _dbContextFactory) : ControllerBase
    {
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
            // Hàm utility để tạo context mới cho mỗi truy vấn
            async Task<T> ExecuteQuery<T>(Func<DataContext, Task<T>> queryFunc)
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await queryFunc(context);
            }

            // Kiểm tra dữ liệu thực tế
            var trainingData = await ExecuteQuery(c => c.Training.ToListAsync());
            var recuimentData = await ExecuteQuery(c => c.Recuiment.ToListAsync());
            var salaryData = await ExecuteQuery(c => c.Salary.ToListAsync());

            var trainingSum = await ExecuteQuery(c => c.Training.SumAsync(t => (decimal?)t.Cost));
            var recuimentSum = await ExecuteQuery(c => c.Recuiment.SumAsync(r => (decimal?)r.Amount));
            var salarySum = await ExecuteQuery(c => c.Salary.SumAsync(s => (decimal?)s.TotalSalary));

            return Ok(new
            {
                Training = new
                {
                    Count = trainingData.Count,
                    TotalCost = trainingSum ?? 0,
                    Data = trainingData.Select(t => new { t.TrainingId, t.Title, t.Cost })
                },
                Recuiment = new
                {
                    Count = recuimentData.Count,
                    TotalAmount = recuimentSum ?? 0,
                    Data = recuimentData.Select(r => new { r.Id, r.CustomerName, r.Amount, r.OpeningsCount })
                },
                Salary = new
                {
                    Count = salaryData.Count,
                    TotalAmount = salarySum ?? 0,
                    Data = salaryData.Select(s => new { s.SalaryId, s.EmployeeId, s.Amount, s.Date })
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