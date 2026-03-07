namespace Tripzo.DTOs.Passenger
{
    public class PassengerHistoryDTO
    {
        public int BookingId { get; set; }
        public string RouteName { get; set; } // e.g. "Chennai -> Bangalore"
        public string BusNumber { get; set; }
        public DateTime JourneyDate { get; set; }
        public string Status { get; set; } // Confirmed, Cancelled, Refunded
        public decimal Amount { get; set; }
    }
}