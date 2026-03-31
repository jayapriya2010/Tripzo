using System.ComponentModel.DataAnnotations;

namespace Tripzo.DTOs.Passenger
{
    public class SearchBusDTO
    {
        [Required(ErrorMessage = "From city is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "From city must be between 2 and 100 characters.")]
        public string FromCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "To city is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "To city must be between 2 and 100 characters.")]
        public string ToCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Travel date is required.")]
        public DateTime TravelDate { get; set; }

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Optional filters
        public string? BusType { get; set; }
        public decimal? MinFare { get; set; }
        public decimal? MaxFare { get; set; }
        // Comma separated amenities, e.g. "WiFi,AC"
        public string? Amenities { get; set; }
    }
}