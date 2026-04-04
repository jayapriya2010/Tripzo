namespace Tripzo.DTOs.Passenger
{
    public class BookingFilterDTO
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } = "latest"; // latest, oldest, highPrice, lowPrice
    }
}
