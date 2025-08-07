using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetUsers()
        {
            // This is just a placeholder. In a real application, you would retrieve users from a database.
            var users = new List<string> { "User1", "User2", "User3" };
            return Ok(users);
        }
        
        [HttpGet("{id}")]
        public ActionResult<string> GetUserById(int id)
        {
            // This is just a placeholder. In a real application, you would retrieve the user from a database.
            if (id <= 0)
            {
                return BadRequest("Invalid user ID.");
            }
            var user = $"User{id}";
            return Ok(user);
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
    }
}