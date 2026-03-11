using System.ComponentModel.DataAnnotations;

namespace Tripzo.DTOs
{
    public class UserRegisterDTO
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", 
            ErrorMessage = "Please enter a valid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", 
            ErrorMessage = "Please enter a valid phone number (E.164 format)")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression(@"^(Admin|Operator|Passenger)$", 
            ErrorMessage = "Role must be either Admin, Operator, or Passenger")]
        public string Role { get; set; } // "Admin", "Operator", or "Passenger"

        [StringLength(20)]
        [RegularExpression(@"^(Male|Female|Other|PreferNotToSay)$", 
            ErrorMessage = "Gender must be Male, Female, Other, or PreferNotToSay")]
        public string? Gender { get; set; }
    }
}