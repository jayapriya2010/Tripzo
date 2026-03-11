using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tripzo.Models;
using Tripzo.DTOs;
using Tripzo.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserController(AppDbContext context, PasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            if (dto == null)
                return BadRequest("Invalid request payload.");

            // Only allow Passenger and Operator to register through this endpoint
            var allowedRoles = new[] { "Passenger", "Operator" };
            if (string.IsNullOrWhiteSpace(dto.Role) || !allowedRoles.Any(r => r.Equals(dto.Role, StringComparison.OrdinalIgnoreCase)))
                return BadRequest("Invalid role. Only 'Passenger' and 'Operator' can register.");

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("User with this email already exists.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role,
                Gender = dto.Gender
            };

            // Hash the password before storing in the database
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{dto.Role} registered successfully!" });
        }
    }
}