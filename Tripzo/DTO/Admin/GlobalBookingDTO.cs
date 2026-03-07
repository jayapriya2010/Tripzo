namespace Tripzo.DTO.Admin
{
    public class GlobalBookingDTO
    {
        public int BookingId { get; set; }
        public string PassengerName { get; set; }
        public string RouteName { get; set; } // e.g., "Chennai to Bangalore"
        public DateTime JourneyDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
}