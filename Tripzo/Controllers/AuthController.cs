using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.DTO.Auth;
using Tripzo.Models;
using Tripzo.Services;

namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(AppDbContext context, JwtService jwtService, PasswordHasher<User> passwordHasher)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
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
    }
}
