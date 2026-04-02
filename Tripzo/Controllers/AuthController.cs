using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.DTO.Auth;
using Tripzo.Models;
using Tripzo.Services;

using Microsoft.Extensions.Caching.Memory;

namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        public AuthController(
            AppDbContext context, 
            JwtService jwtService, 
            PasswordHasher<User> passwordHasher,
            IMemoryCache cache,
            IEmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _cache = cache;
            _emailService = emailService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (dto == null)
                return BadRequest("Invalid login request.");

            // Trim and lowercase the email for consistent lookup
            var searchEmail = dto.Email.Trim().ToLower();

            // Find user by email only — role is auto-detected
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == searchEmail);

            if (user == null)
                return Unauthorized("Invalid email or password.");

            // Check if user account is active
            if (!user.IsActive)
                return Unauthorized("Your account has been deactivated. Please contact the administrator.");

            // Verify the hashed password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid email or password.");

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);

            return Ok(new LoginResponseDTO
            {
                UserId = user.UserId,
                Token = token,
                Email = user.Email,
                Role = user.Role,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender
            });
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Email))
                return BadRequest("Email is required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null)
            {
                // To prevent email enumeration, we return Ok even if the user doesn't exist
                return Ok(new { message = "If your email is registered, you will receive a verification code shortly." });
            }

            // RESTRICTION: Forgot password is not allowed for Admin role
            if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Password reset via OTP is restricted for administrator accounts. Please contact system support.");
            }

            // Generate 6-digit OTP
            string otp = Random.Shared.Next(100000, 999999).ToString();

            // Store in cache for 10 minutes
            var cacheKey = $"OTP_{dto.Email.ToLower()}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(10));

            // Send Email
            await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otp);

            return Ok(new { message = "Verification code sent successfully!" });
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Otp))
                return BadRequest("Email and OTP are required.");

            var cacheKey = $"OTP_{dto.Email.ToLower()}";
            if (!_cache.TryGetValue(cacheKey, out string? cachedOtp))
            {
                return BadRequest("Verification code has expired. Please request a new one.");
            }

            if (cachedOtp != dto.Otp)
            {
                return BadRequest("Invalid verification code. Please check the code sent to your email.");
            }

            return Ok(new { message = "OTP verified successfully!" });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var cacheKey = $"OTP_{dto.Email.ToLower()}";
            if (!_cache.TryGetValue(cacheKey, out string? cachedOtp))
            {
                return BadRequest("Verification code has expired. Please request a new one.");
            }

            if (cachedOtp != dto.Otp)
            {
                return BadRequest("Invalid verification code. Please check the code sent to your email.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null)
                return NotFound("User not found.");

            // Hash and update password
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            
            await _context.SaveChangesAsync();

            // Remove OTP from cache after use
            _cache.Remove(cacheKey);

            return Ok(new { message = "Password reset successfully! You can now log in with your new password." });
        }
    }
}
