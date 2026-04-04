namespace Tripzo.DTOs.Operator
{
    public class PaginationFilterDTO
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } // e.g. "latest", "name"
        public string? Status { get; set; } // e.g. "Active", "Inactive"
        public string? Category { get; set; } // e.g. "AC", "Non-AC"
        public string? Type { get; set; } // e.g. "Sleeper", "Seater"
        public DateTime? FilterDate { get; set; }
    }
}
