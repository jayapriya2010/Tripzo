namespace Tripzo.DTO.Auth
{
    public class LoginResponseDTO
    {
        public required string Token { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public required string FullName { get; set; }
    }
}
