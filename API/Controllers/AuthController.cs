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
    public class AuthController(IUserRepository _userRepository, TokenService _tokenService, ILogger<AuthController> _logger, IConfiguration _configuration) : BaseApiController
    {   
        private readonly IUserRepository _userRepository = _userRepository;
        private readonly TokenService _tokenService = _tokenService;
        private readonly ILogger<AuthController> _logger = _logger;
        private readonly IConfiguration _configuration = _configuration;
    

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LogInDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.UserName)  ||
               string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return BadRequest(new {message = "Username va password la bat buoc"});
            }

            var user = await _userRepository.GetUserEntityByUsernameAsync(loginDto.UserName);
            if(user == null)
            {
                _logger.LogWarning("Login failed : Username not found");
                return Unauthorized(new { message = "Invalid username or password"});
            }

            if (!VerifyPasswordHash(loginDto.Password, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Login failed: password mismatch.");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            var token = _tokenService.CreateToken(user);

            string BuildAvatarUrl(User u)
            {
                if (string.IsNullOrEmpty(u.Image) || u.Image == "default.png")
                    return "/assets/default-avatar.png";


                if (u.Image == "admin-avatar.png" || u.Image == "manager-avatar.png" || u.Image == "employee-avatar.png")
                    return $"/assets/avatars/{u.Image}";


                return $"{Request.Scheme}://{Request.Host}/uploads/avatars/{u.Image}";
            }
            
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
                    avatarUrl = BuildAvatarUrl(user),
                    displayName = user.UserName,
                    role = user.Role ?? "user" 
                }
            });
        }


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
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "No token provided." });
            }

            var token = authHeader["Bearer ".Length..];

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["TokenKey"]);

            try
            {
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

        private static bool VerifyPasswordHash(string password, byte[] hash, byte[] salt)
        {
            using var hmac = new HMACSHA512(salt);
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computed.SequenceEqual(hash);
        }
    }
}
