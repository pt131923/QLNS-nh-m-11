using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using Microsoft.AspNetCore.Mvc;
using API.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(IUserRepository _userRepository) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            var users = await _userRepository.GetUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetUserById(int id)
        {
            // This is just a placeholder. In a real application, you would retrieve the user from a database.
            if (id <= 0)
            {
                return BadRequest("Invalid user ID.");
            }
            var users = await _userRepository.GetUserByIdAsync(id);
            return Ok(users);
        }

        [HttpPost]
        public ActionResult<string> CreateUser([FromBody] string userName)
        {
            // This is just a placeholder. In a real application, you would save the user to a database.
            if (string.IsNullOrEmpty(userName))
            {
                return BadRequest("User name cannot be empty.");
            }
            return CreatedAtAction(nameof(GetUserById), new { id = 1 }, userName);
        }

        [HttpPut("{id}")]
        public ActionResult<string> UpdateUser(int id, [FromBody] string userName)
        {
            // This is just a placeholder. In a real application, you would update the user in a database.
            if (id <= 0 || string.IsNullOrEmpty(userName))
            {
                return BadRequest("Invalid user ID or user name.");
            }
            return Ok($"User{id} updated to {userName}");
        }

        [HttpDelete("{id}")]
        public ActionResult<string> DeleteUser(int id)
        {
            // This is just a placeholder. In a real application, you would delete the user from a database.
            if (id <= 0)
            {
                return BadRequest("Invalid user ID.");
            }
            return Ok($"User{id} deleted");
        }

        [HttpGet("search")]
        public ActionResult<IEnumerable<string>> SearchUsers(string query)
        {
            // This is just a placeholder. In a real application, you would search users in a database.
            var users = new List<string> { "User1", "User2", "User3" };
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Search query cannot be empty.");
            }
            var results = users.Where(u => u.Contains(query, StringComparison.OrdinalIgnoreCase));
            return Ok(results);
        }
    }
}