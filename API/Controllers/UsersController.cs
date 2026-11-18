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
    public class UpdatePasswordDto
    {
        public string Password { get; set; }
        public string NewPassword { get; set; }
    }

    public class CreateUserRequestDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Role { get; set; } = "user";
        public int? EmployeeId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateRoleRequestDto
    {
        public string Role { get; set; }
    }

    public class UpdateStatusRequestDto
    {
        public bool IsActive { get; set; }
    }

    public class UpdateEmployeeLinkRequestDto
    {
        public int? EmployeeId { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : BaseApiController
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

        [Authorize]
[HttpPost("me/avatar")]
[RequestSizeLimit(10_000_000)]
public async Task<ActionResult> UploadMyAvatar([FromForm] FileDto dto)
{
    if (dto.File == null || dto.File.Length == 0)
        return BadRequest(new { message = "No file uploaded." });
    
    var username = User.FindFirstValue(ClaimTypes.Name);
    if (string.IsNullOrEmpty(username))
        return Unauthorized(new { message = "Cannot read username from token." });
    
    var userEntity = await _userRepository.GetUserByUsernameAsync(username);
    if (userEntity == null)
        return NotFound(new { message = "User not found." });
    
    var ext = Path.GetExtension(dto.File.FileName)?.ToLowerInvariant();
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
        await dto.File.CopyToAsync(stream);
    }
    
    userEntity.Image = fileName;
    var users = _db.GetCollection<User>("Users");
    await users.ReplaceOneAsync(u => u.UserId == userEntity.UserId, userEntity);
    
    var avatarUrl = BuildAvatarUrl(fileName);
    return Ok(new { AvatarUrl = avatarUrl, Image = fileName });
}


        [Authorize]
        [HttpPut("{id}/password")]
        public async Task<ActionResult> UpdatePassword(int id, [FromForm] UpdatePasswordDto dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID." });
            
            if (dto == null || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Password and newPassword are required." });
            
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { message = "Token missing or invalid." });
            
            var userEntity = await _userRepository.GetUserByUsernameAsync(username);
            if (userEntity == null)
                return NotFound(new { message = "User not found." });
            
            if (userEntity.UserId != id)
                return Forbid();
            
            if (!VerifyPasswordHash(dto.Password, userEntity.PasswordHash, userEntity.PasswordSalt))
                return Unauthorized(new { message = "Old password is incorrect." });
            
            using var hmac = new System.Security.Cryptography.HMACSHA512();
            userEntity.PasswordSalt = hmac.Key;
            userEntity.PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.NewPassword));
            
            var users = _db.GetCollection<User>("Users");
            await users.ReplaceOneAsync(u => u.UserId == userEntity.UserId, userEntity);
            
            return Ok(new { message = "Password updated successfully." });
        }

        private static bool VerifyPasswordHash(string password, byte[] hash, byte[] salt)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(salt);
            var computed = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computed.SequenceEqual(hash);
        }

        [HttpGet]
        public async Task<ActionResult> GetUsers([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string q)
        {
            var usersCol = _db.GetCollection<User>("Users");
            var filter = Builders<User>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                var regex = new MongoDB.Bson.BsonRegularExpression(term, "i");
                filter = Builders<User>.Filter.Or(
                    Builders<User>.Filter.Regex(u => u.UserName, regex),
                    Builders<User>.Filter.Regex(u => u.Email, regex),
                    Builders<User>.Filter.Regex(u => u.PhoneNumber, regex),
                    Builders<User>.Filter.Regex(u => u.Role, regex)
                );
            }

            var total = await usersCol.CountDocumentsAsync(filter);
            var query = usersCol.Find(filter).SortBy(u => u.UserId);

            List<User> list;
            if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
            {
                list = await query.Skip((page.Value - 1) * pageSize.Value).Limit(pageSize.Value).ToListAsync();
            }
            else
            {
                list = await query.ToListAsync();
            }

            var dtos = list.Select(u => new UserDto
            {
                UserId = u.UserId,
                EmployeeId = u.EmployeeId,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Role = u.Role ?? "user",
                Image = u.Image ?? "default.png",
                AvatarUrl = BuildAvatarUrl(u.Image),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();

            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(new { items = dtos, total, page = page.Value, pageSize = pageSize.Value });
            }

            return Ok(dtos);
        }

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

        [HttpPost]
        public async Task<ActionResult> AddUser([FromBody] CreateUserRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest(new { message = "Username cannot be empty." });

            var usersCol = _db.GetCollection<User>("Users");
            var existing = await usersCol.Find(u => u.UserName == dto.UserName.Trim()).FirstOrDefaultAsync();
            if (existing != null)
                return BadRequest(new { message = $"User '{dto.UserName}' already exists." });

            var lastUser = await usersCol.Find(_ => true)
                .SortByDescending(u => u.UserId)
                .FirstOrDefaultAsync();
            var newId = (lastUser?.UserId ?? 0) + 1;

            byte[] passwordHash = null;
            byte[] passwordSalt = null;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                using var hmac = new System.Security.Cryptography.HMACSHA512();
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.Password));
            }

            var newUser = new User
            {
                UserId = newId,
                UserName = dto.UserName.Trim(),
                Email = dto.Email?.Trim() ?? string.Empty,
                PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
                Address = dto.Address?.Trim() ?? string.Empty,
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "user" : dto.Role.Trim().ToLowerInvariant(),
                EmployeeId = dto.EmployeeId ?? 0,
                IsActive = dto.IsActive,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Image = "default.png",
                CreatedAt = DateTime.UtcNow
            };

            await usersCol.InsertOneAsync(newUser);
            return Ok(new { message = "User created successfully.", userId = newId });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UserDto userDto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid user ID." });
            if (userDto == null) return BadRequest(new { message = "Data is required." });

            var usersCol = _db.GetCollection<User>("Users");
            var updateDef = Builders<User>.Update
                .Set(u => u.UserName, userDto.UserName)
                .Set(u => u.Email, userDto.Email)
                .Set(u => u.PhoneNumber, userDto.PhoneNumber)
                .Set(u => u.Address, userDto.Address)
                .Set(u => u.Role, string.IsNullOrWhiteSpace(userDto.Role) ? "user" : userDto.Role.Trim().ToLowerInvariant())
                .Set(u => u.EmployeeId, userDto.EmployeeId)
                .Set(u => u.IsActive, userDto.IsActive);

            var result = await usersCol.UpdateOneAsync(u => u.UserId == id, updateDef);
            if (result.MatchedCount == 0)
                return NotFound(new { message = $"User with ID {id} not found." });

            return Ok(new { message = "User updated successfully." });
        }

        [HttpPut("{id}/role")]
        public async Task<ActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequestDto dto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid user ID." });
            if (dto == null || string.IsNullOrWhiteSpace(dto.Role))
                return BadRequest(new { message = "Role is required." });

            var usersCol = _db.GetCollection<User>("Users");
            var update = Builders<User>.Update.Set(u => u.Role, dto.Role.Trim().ToLowerInvariant());
            var result = await usersCol.UpdateOneAsync(u => u.UserId == id, update);

            if (result.MatchedCount == 0) return NotFound(new { message = $"User with ID {id} not found." });
            return Ok(new { message = "Role updated successfully." });
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid user ID." });

            var usersCol = _db.GetCollection<User>("Users");
            var update = Builders<User>.Update.Set(u => u.IsActive, dto.IsActive);
            var result = await usersCol.UpdateOneAsync(u => u.UserId == id, update);

            if (result.MatchedCount == 0) return NotFound(new { message = $"User with ID {id} not found." });
            return Ok(new { message = "Status updated successfully." });
        }

        [HttpPut("{id}/employee-link")]
        public async Task<ActionResult> UpdateEmployeeLink(int id, [FromBody] UpdateEmployeeLinkRequestDto dto)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid user ID." });

            var usersCol = _db.GetCollection<User>("Users");
            var update = Builders<User>.Update.Set(u => u.EmployeeId, dto?.EmployeeId ?? 0);
            var result = await usersCol.UpdateOneAsync(u => u.UserId == id, update);

            if (result.MatchedCount == 0) return NotFound(new { message = $"User with ID {id} not found." });
            return Ok(new { message = "Employee link updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID." });

            var usersCol = _db.GetCollection<User>("Users");
            var result = await usersCol.DeleteOneAsync(u => u.UserId == id);
            if (result.DeletedCount == 0)
                return NotFound(new { message = $"User with ID {id} not found." });

            return Ok(new { message = "User deleted successfully." });
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");
            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            
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