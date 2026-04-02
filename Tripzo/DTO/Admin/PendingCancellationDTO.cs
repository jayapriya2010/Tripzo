namespace Tripzo.DTO.Admin
{
    public class PendingCancellationDTO
    {
        public int BookingId { get; set; }
        public string PassengerName { get; set; }
        public string PassengerEmail { get; set; }
        public string RouteName { get; set; }
        public string BusNumber { get; set; }
        public DateTime JourneyDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CancellationDate { get; set; }
        public string Status { get; set; }
        public string? CancellationReason { get; set; }
        public List<BookedSeatDetailDTO>? BookedSeats { get; set; }
    }

    public class BookedSeatDetailDTO
    {
        public int BookedSeatId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string Status { get; set; } = "Confirmed";
        public string? CancellationReason { get; set; }
    }
}
