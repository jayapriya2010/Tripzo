namespace Tripzo.DTO.Auth
{
    public class LoginResponseDTO
    {
        public int UserId { get; set; }
        public required string Token { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public required string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
    }
}
