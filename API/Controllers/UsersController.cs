using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using API.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // ------------------------------------------------------------
        // GET CURRENT USER (LẤY TỪ JWT TOKEN)
        // ------------------------------------------------------------
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(username))
                return Unauthorized("Cannot read username from token.");

            var user = await _userRepository.GetUserByNameAsync(username);

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }

        // ------------------------------------------------------------
        // GET ALL USERS
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userRepository.GetUsersAsync();
            return Ok(users);
        }

        // ------------------------------------------------------------
        // GET BY ID
        // ------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid user ID.");

            var user = await _userRepository.GetUserByIdAsync(id);

            if (user == null)
                return NotFound($"User with ID {id} not found.");

            return Ok(user);
        }

        // ------------------------------------------------------------
        // GET BY NAME
        // ------------------------------------------------------------
        [HttpGet("by-name/{userName}")]
        public async Task<ActionResult<UserDto>> GetUserByName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return BadRequest("Username cannot be empty.");

            var user = await _userRepository.GetUserByNameAsync(userName);

            if (user == null)
                return NotFound($"User '{userName}' not found.");

            return Ok(user);
        }

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult> AddUser([FromBody] UserDto userDto)
        {
            if (userDto == null || string.IsNullOrWhiteSpace(userDto.UserName))
                return BadRequest("User name cannot be empty.");

            if (await _userRepository.UserExistsAsync(userDto.UserName))
                return BadRequest($"User '{userDto.UserName}' already exists.");

            var success = await _userRepository.AddUserAsync(userDto);

            if (!success)
                return BadRequest("Failed to create user.");

            await _userRepository.SaveChangesAsync();

            return Ok("User created successfully.");
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UserDto userDto)
        {
            if (id != userDto.UserId)
                return BadRequest("Route ID and body ID do not match.");

            var success = await _userRepository.UpdateUserAsync(userDto);

            if (!success)
                return NotFound($"User with ID {userDto.UserId} not found.");

            await _userRepository.SaveChangesAsync();

            return Ok("User updated successfully.");
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid user ID.");

            var success = await _userRepository.DeleteUserAsync(id);

            if (!success)
                return NotFound($"User with ID {id} not found.");

            await _userRepository.SaveChangesAsync();

            return Ok("User deleted successfully.");
        }
    }
}
