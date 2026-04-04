namespace Tripzo.DTOs.Passenger
{
    public class CancellationResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public DateTime JourneyDate { get; set; }
        public decimal Amount { get; set; }
        public string SeatNumbers { get; set; } = string.Empty;
    }
}
