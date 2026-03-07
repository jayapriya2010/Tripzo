namespace Tripzo.DTOs.Passenger
{
    public class CancelBookingDTO
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string Reason { get; set; }
    }
}