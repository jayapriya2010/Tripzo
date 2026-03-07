using System.ComponentModel.DataAnnotations;
using Tripzo.DTO;

namespace Tripzo.DTO.Admin
{
    public class UserListDTO
    {
        public int UserId { get; set; }
        
        public string FullName { get; set; }
        
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", 
            ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }
        
        public string Role { get; set; } // Admin, Operator, Passenger

        public string Gender { get; set; }
        
        public bool IsActive { get; set; }
    }
}
