using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using API.Interfaces;
using API.Entities;
using MongoDB.Driver;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMongoDatabase _db;
        private readonly IWebHostEnvironment _env;

        public UsersController(IUserRepository userRepository, IMongoDatabase db, IWebHostEnvironment env)
        {
            _userRepository = userRepository;
            _db = db;
            _env = env;
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

            // Build AvatarUrl (giống logic login)
            user.AvatarUrl = BuildAvatarUrl(user.Image);

            return Ok(user);
        }

        private string BuildAvatarUrl(string image)
        {
            if (string.IsNullOrEmpty(image) || image == "default.png")
                return "/assets/default-avatar.png";
            if (image == "admin-avatar.png" || image == "manager-avatar.png" || image == "employee-avatar.png")
                return $"/assets/avatars/{image}";
            return $"{Request.Scheme}://{Request.Host}/uploads/avatars/{image}";
        }

        // ------------------------------------------------------------
        // UPLOAD AVATAR (CURRENT USER)
        // ------------------------------------------------------------
        [Authorize]
        [HttpPost("me/avatar")]
        [RequestSizeLimit(10_000_000)] // 10MB
        public async Task<ActionResult> UploadMyAvatar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { message = "Cannot read username from token." });

            var userEntity = await _userRepository.GetUserEntityByUsernameAsync(username);
            if (userEntity == null)
                return NotFound(new { message = "User not found." });

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                return BadRequest(new { message = "Only .jpg, .jpeg, .png, .webp files are allowed." });

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
                webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");

            var avatarDir = Path.Combine(webRoot, "uploads", "avatars");
            Directory.CreateDirectory(avatarDir);

            var fileName = $"user_{userEntity.UserId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(avatarDir, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            // Update Mongo user image
            userEntity.Image = fileName;
            var users = _db.GetCollection<User>("Users");
            await users.ReplaceOneAsync(u => u.UserId == userEntity.UserId, userEntity);

            var avatarUrl = BuildAvatarUrl(fileName);
            return Ok(new { AvatarUrl = avatarUrl, Image = fileName });
        }

        // ------------------------------------------------------------
        // UPDATE PASSWORD
        // ------------------------------------------------------------
        public class UpdatePasswordDto
        {
            public string Password { get; set; }
            public string NewPassword { get; set; }
        }

        [Authorize]
        [HttpPut("{id}/password")]
        public async Task<ActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordDto dto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid user ID." });
            if (dto == null || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Password and newPassword are required." });

            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { message = "Token missing or invalid." });

            var userEntity = await _userRepository.GetUserEntityByUsernameAsync(username);
            if (userEntity == null) return NotFound(new { message = "User not found." });
            if (userEntity.UserId != id) return Forbid();

            if (!VerifyPasswordHash(dto.Password, userEntity.PasswordHash, userEntity.PasswordSalt))
                return Unauthorized(new { message = "Old password is incorrect." });

            using var hmac = new System.Security.Cryptography.HMACSHA512();
            userEntity.PasswordSalt = hmac.Key;
            userEntity.PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.NewPassword));

            var users = _db.GetCollection<User>("Users");
            await users.ReplaceOneAsync(u => u.UserId == userEntity.UserId, userEntity);

            return Ok(new { message = "Password updated successfully." });
        }

        private bool VerifyPasswordHash(string password, byte[] hash, byte[] salt)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(salt);
            var computed = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computed.SequenceEqual(hash);
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

        [HttpPost("me/avatar")]
public async Task<IActionResult> UploadAvatar(IFormFile file)
{
    if (file == null || file.Length == 0)
    {
        return BadRequest("No file uploaded");
    }

    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/avatars");

    if (!Directory.Exists(uploadsFolder))
    {
        Directory.CreateDirectory(uploadsFolder);
    }

    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
    var filePath = Path.Combine(uploadsFolder, fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var avatarUrl = "/avatars/" + fileName;

    return Ok(new { avatarUrl });
}
    }
}
