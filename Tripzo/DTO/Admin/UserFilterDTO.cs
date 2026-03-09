namespace Tripzo.DTO.Admin
{
    public class UserFilterDTO
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Role { get; set; } // Filter by "Operator" or "Passenger"
        public bool? IsActive { get; set; } // Filter by active status
        public string? SearchTerm { get; set; } // Search by name or email
        public string? Gender { get; set; } // Filter by gender
        public string SortBy { get; set; } = "FullName"; // Sort by column
        public bool SortDescending { get; set; } = false;
    }
}
