using System.Security.Claims;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TimeKeepingController(
        ITimeKeepingRepository timekeepingRepository,
        IUserRepository userRepository,
        IMongoCollection<Employee> employees
    ) : ControllerBase
    {
        [HttpGet("history")]
        public async Task<IActionResult> GetMyTimeEntries()
        {
            var employeeId = await ResolveEmployeeId();
            if (employeeId == null)
                return MissingEmployeeResult();

            var entries = await timekeepingRepository
                .GetTimeEntriesForUser(employeeId);

            return Ok(entries);
        }

        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn()
        {
            var employeeId = await ResolveEmployeeId();
            if (employeeId == null)
                return MissingEmployeeResult();

            try
            {
                await timekeepingRepository.CheckIn(employeeId);
                return Ok(new { message = "Chấm công vào thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("check-out")]
        public async Task<IActionResult> CheckOut()
        {
            var employeeId = await ResolveEmployeeId();
            if (employeeId == null)
                return MissingEmployeeResult();

            try
            {
                await timekeepingRepository.CheckOut(employeeId);
                return Ok(new { message = "Chấm công ra thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("timekeeping-with-employees")]
        public async Task<IActionResult> GetTimekeepingWithEmployees()
        {
            var result = await timekeepingRepository.GetTimekeepingWithEmployees();
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("timekeeping-with-employees-by-date")]
        public async Task<IActionResult> GetTimekeepingWithEmployeesByDate(DateTime date)
        {
            var result = await timekeepingRepository.GetTimekeepingWithEmployeesByDate(date);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        private async Task<string> ResolveEmployeeId()
        {
            var userIdValue = User.FindFirst("userId")?.Value
                ?? User.FindFirst("employeeId")?.Value;
            var username = User.FindFirstValue(ClaimTypes.Name);

            UserDto user = null;

            if (int.TryParse(userIdValue, out var userId) && userId > 0)
                user = await userRepository.GetUserByIdAsync(userId);

            if (user == null && !string.IsNullOrWhiteSpace(username))
                user = await userRepository.GetUserByNameAsync(username);

            if (user == null)
                return null;

            Employee employee = null;

            if (int.TryParse(User.FindFirst("employeeId")?.Value, out var claimedEmployeeId) && claimedEmployeeId > 0)
            {
                employee = await employees
                    .Find(x => x.EmployeeId == claimedEmployeeId)
                    .FirstOrDefaultAsync();
            }

            if (employee == null && user.EmployeeId > 0)
            {
                employee = await employees
                    .Find(x => x.EmployeeId == user.EmployeeId)
                    .FirstOrDefaultAsync();
            }

            if (employee == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                employee = await employees
                    .Find(x => x.EmployeeEmail == user.Email)
                    .FirstOrDefaultAsync();
            }

            if (employee == null && !string.IsNullOrWhiteSpace(user.UserName))
            {
                employee = await employees
                    .Find(x => x.EmployeeName == user.UserName || x.EmployeeEmail == user.UserName)
                    .FirstOrDefaultAsync();
            }

            return employee?.EmployeeId.ToString();
        }

        private IActionResult MissingEmployeeResult()
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            return BadRequest(new
            {
                message = "Tài khoản chưa liên kết hồ sơ nhân viên nên không thể chấm công."
            });
        }
    }
}
