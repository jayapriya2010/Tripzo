namespace Tripzo.DTOs.Passenger
{
    public class BusSearchResultDTO
    {
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public string BusName { get; set; }
        public string BusType { get; set; } // Sleeper, AC, etc.
        public TimeSpan DepartureTime { get; set; }
        public decimal Fare { get; set; }
        public List<string> Amenities { get; set; }
        public int AvailableSeats { get; set; }
        public double? AverageRating { get; set; } // Average rating from feedbacks
        public int TotalReviews { get; set; } // Number of reviews
    }
}