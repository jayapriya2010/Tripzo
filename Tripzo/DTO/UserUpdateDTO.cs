using System.ComponentModel.DataAnnotations;

namespace Tripzo.DTOs
{
    public class UserUpdateDTO
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", 
            ErrorMessage = "Please enter a valid phone number (E.164 format)")]
        public string? PhoneNumber { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^(Male|Female|Other|PreferNotToSay)$", 
            ErrorMessage = "Gender must be Male, Female, Other, or PreferNotToSay")]
        public string? Gender { get; set; }
    }
}
