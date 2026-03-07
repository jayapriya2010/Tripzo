namespace Tripzo.DTOs.Passenger
{
    public class BusSearchResultDTO
    {
        public int RouteId { get; set; }
        public string BusName { get; set; }
        public string BusType { get; set; } // Sleeper, AC, etc.
        public TimeSpan DepartureTime { get; set; }
        public decimal Fare { get; set; }
        public List<string> Amenities { get; set; } // Added to meet requirement
        public int AvailableSeats { get; set; }
    }
}