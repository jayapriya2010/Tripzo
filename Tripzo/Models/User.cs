using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", 
            ErrorMessage = "Please enter a valid email address")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } // "Admin", "Operator", "Passenger"

        [StringLength(20)]
        [RegularExpression(@"^(Male|Female|Other|PreferNotToSay)$", 
            ErrorMessage = "Gender must be Male, Female, Other, or PreferNotToSay")]
        public string Gender { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Bus> ManagedBuses { get; set; } = new List<Bus>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
