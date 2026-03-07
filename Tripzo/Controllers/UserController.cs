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

        public UserController(AppDbContext context)
        {
            _context = context;
        }

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
                PasswordHash = dto.Password,
                Role = dto.Role,
                Gender = dto.Gender
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{dto.Role} registered successfully!" });
        }
    }
}