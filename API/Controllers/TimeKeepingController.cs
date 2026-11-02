
using System.Security.Claims;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TimeKeepingController(
        ITimeKeepingRepository timekeepingRepository
    ) : ControllerBase
    {
        // Lấy lịch sử chấm công của tài khoản đang đăng nhập
        [HttpGet]
        public async Task<IActionResult> GetMyTimeEntries()
        {
            var employeeId = GetEmployeeId();

            if (employeeId == null)
                return Unauthorized();

            var entries = await timekeepingRepository
                .GetTimeEntriesForUser(employeeId);

            return Ok(entries);
        }

        // Check-in
        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn()
        {
            var employeeId = GetEmployeeId();

            if (employeeId == null)
                return Unauthorized();

            var result = await timekeepingRepository
                .CheckIn(employeeId);

            return Ok(result);
        }

        // Check-out
        [HttpPost("check-out")]
        public async Task<IActionResult> CheckOut()
        {
            var employeeId = GetEmployeeId();

            if (employeeId == null)
                return Unauthorized();

            var result = await timekeepingRepository
                .CheckOut(employeeId);

            return Ok(result);
        }

        [HttpGet("timekeeping-with-employees")]
        public async Task<IActionResult> GetTimekeepingWithEmployees()
        {
            var result = await timekeepingRepository
                .GetTimekeepingWithEmployees();

            return Ok(result);
        }

        private string GetEmployeeId()
        {
            // Điều chỉnh claim theo cấu trúc JWT của project.
            return User.FindFirst("employeeId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
