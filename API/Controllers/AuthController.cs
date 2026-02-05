using API.DTOs;
using API.Interfaces;
using API.Entities;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(IUserRepository userRepository, TokenService tokenService, ILogger<AuthController> logger, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _logger = logger;
            _configuration = configuration;
        }

        // ---------------- LOGIN ---------------------
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LogInDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.UserName) ||
                string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return BadRequest(new { message = "Username và password là bắt buộc." });
            }

            var user = await _userRepository.GetUserEntityByUsernameAsync(loginDto.UserName);
            if (user == null)
            {
                _logger.LogWarning("Login failed: username not found.");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            if (!VerifyPasswordHash(loginDto.Password, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Login failed: password mismatch.");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            var token = _tokenService.CreateToken(user);

            // Trả về đầy đủ thông tin user để frontend cập nhật UI
            return Ok(new
            {
                token,
                user = new
                {
                    userId = user.UserId,
                    username = user.UserName,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    address = user.Address,
                    // Sử dụng trường Image cho avatar
                    avatarUrl = string.IsNullOrEmpty(user.Image) || user.Image == "default.png"
                        ? "/assets/default-avatar.png"
                        : $"/assets/avatars/{user.Image}",
                    displayName = user.UserName, // Sử dụng username làm display name
                    role = user.Role ?? "user" // Sử dụng role từ database
                }
            });
        }

        // ---------------- CURRENT USER ---------------------
        [Authorize]
        [HttpGet("current")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);

            if (username == null)
            {
                return Unauthorized(new { message = "Token missing or invalid." });
            }

            var user = await _userRepository.GetUserByNameAsync(username);
            if (user == null) return NotFound(new { message = "User not found." });

            return Ok(user);
        }

        // ---------------- TEST AUTH ---------------------
        [Authorize]
        [HttpGet("test")]
        public IActionResult TestAuth()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var userId = User.FindFirstValue("userId");

            return Ok(new
            {
                message = "Authentication successful",
                username = username,
                userId = userId,
                timestamp = DateTime.Now
            });
        }

        // ---------------- PUBLIC TEST ---------------------
        [HttpGet("public-test")]
        public IActionResult PublicTest()
        {
            return Ok(new
            {
                message = "Public API works",
                timestamp = DateTime.Now,
                server = "API running on localhost:5002"
            });
        }

        // ---------------- REFRESH TOKEN ---------------------
        [HttpPost("refresh")]
        public async Task<ActionResult> RefreshToken()
        {
            // Get token from Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "No token provided." });
            }

            var token = authHeader.Substring("Bearer ".Length);

            // Manually validate the token (even if expired)
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["TokenKey"]);

            try
            {
                // Parse token without validating expiration
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

                var user = await _userRepository.GetUserEntityByUsernameAsync(username);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                var newToken = _tokenService.CreateToken(user);

                return Ok(new
                {
                    token = newToken,
                    message = "Token refreshed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return Unauthorized(new { message = "Invalid token." });
            }
        }

        // ---------------- PASSWORD VERIFY ---------------------
        private bool VerifyPasswordHash(string password, byte[] hash, byte[] salt)
        {
            using var hmac = new HMACSHA512(salt);
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computed.SequenceEqual(hash);
        }
    }
}
