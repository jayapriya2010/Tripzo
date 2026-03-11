namespace Tripzo.DTO.Admin
{
    public class UserDetailsDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; }
        public string? Gender { get; set; }
        public bool IsActive { get; set; }
    }
}
