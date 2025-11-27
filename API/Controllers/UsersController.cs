using System.Collections.Generic;
using System.Threading.Tasks;
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

        // --------------------------------------------------
        // GET: /api/users
        // --------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userRepository.GetUsersAsync();
            return Ok(users);
        }

        // --------------------------------------------------
        // GET: /api/users/{id}
        // --------------------------------------------------
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

        // --------------------------------------------------
        // GET: /api/users/by-name/{username}
        // --------------------------------------------------
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

        // --------------------------------------------------
        // POST: /api/users
        // --------------------------------------------------
        [HttpPost]
        public async Task<ActionResult> AddUser([FromBody] UserDto userDto)
        {
            if (userDto == null || string.IsNullOrWhiteSpace(userDto.UserName))
                return BadRequest("User name cannot be empty.");

            // Check user exists
            if (await _userRepository.UserExistsAsync(userDto.UserName))
                return BadRequest($"User '{userDto.UserName}' already exists.");

            var success = await _userRepository.AddUserAsync(userDto);

            if (!success)
                return BadRequest("Failed to create user.");

            await _userRepository.SaveChangesAsync();

            return Ok("User created successfully.");
        }

        // --------------------------------------------------
        // PUT: /api/users
        // Cập nhật dùng UserDto, không có ID ở route
        // --------------------------------------------------
        [HttpPut]
        public async Task<ActionResult> UpdateUser([FromBody] UserDto userDto)
        {
            if (userDto == null || userDto.UserId <= 0)
                return BadRequest("Invalid user data.");

            var success = await _userRepository.UpdateUserAsync(userDto);

            if (!success)
                return NotFound($"User with ID {userDto.UserId} not found.");

            await _userRepository.SaveChangesAsync();

            return Ok("User updated successfully.");
        }

        // --------------------------------------------------
        // DELETE: /api/users/{id}
        // --------------------------------------------------
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
