namespace Tripzo.DTO.Admin
{
    public class RouteFilterDTO
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SourceCity { get; set; } // Filter by source city
        public string? DestCity { get; set; } // Filter by destination city
        public string? BusName { get; set; } // Filter by bus name
        public string? BusNumber { get; set; } // Filter by bus number
        public decimal? MinFare { get; set; } // Filter by minimum fare
        public decimal? MaxFare { get; set; } // Filter by maximum fare
        public string? SearchTerm { get; set; } // Search by city names or bus details
        public string SortBy { get; set; } = "SourceCity"; // Sort by column
        public bool SortDescending { get; set; } = false;
    }
}
