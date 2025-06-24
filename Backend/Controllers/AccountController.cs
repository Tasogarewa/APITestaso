using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtService _jwtService;

        public AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                Email = dto.Email,
                UserName = dto.FullName,
                PasswordHash = dto.Password
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Registration successful");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized();

            var tokens = await _jwtService.GenerateTokensAsync(user);

            return Ok(new
            {
                accessToken = tokens.accessToken,
                refreshToken = tokens.refreshToken
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            Log.Information($"Отримано запит на оновлення токена: {request.RefreshToken}");
            var refreshTokenEntity = await _jwtService.GetRefreshTokenAsync(request.RefreshToken);
            if (refreshTokenEntity == null || refreshTokenEntity.IsExpired)
            {
                Log.Error($"Недійсний або закінчився refresh token: {request.RefreshToken}");
                return Unauthorized(new { message = "Недійсний або закінчився refresh token." });
            }

            var user = await _userManager.FindByIdAsync(refreshTokenEntity.UserId);
            if (user == null)
            {
                Log.Error($"Користувача не знайдено для UserId: {refreshTokenEntity.UserId}");
                return Unauthorized(new { message = "Користувача не знайдено." });
            }

            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);
            var tokens = await _jwtService.GenerateTokensAsync(user);
            Log.Information($"Оновлено токени для користувача {user.Id}");
            return Ok(new
            {
                accessToken = tokens.accessToken,
                refreshToken = tokens.refreshToken
            });
        }
        [HttpGet("connection-string")]
        public async Task<ActionResult<string>> GetConnectionString()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user.DatabaseConnectionString ?? "");
        }
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.DatabaseConnectionString
            });
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, dto.Email);
                if (!setEmailResult.Succeeded)
                    return BadRequest(setEmailResult.Errors);
            }

            if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, dto.UserName);
                if (!setUserNameResult.Succeeded)
                    return BadRequest(setUserNameResult.Errors);
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, dto.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                    return BadRequest(setPhoneResult.Errors);
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(updateResult.Errors);

            return Ok("Profile updated successfully");
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound("User not found");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password changed successfully");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("Logged out successfully");
        }

        [HttpPost("SetConnectionString")]
        public async Task<IActionResult> SetConnectionString([FromBody] string connectionString)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound();

            user.DatabaseConnectionString = connectionString;
            await _userManager.UpdateAsync(user);

            return Ok("Connection string saved.");
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
    }

}