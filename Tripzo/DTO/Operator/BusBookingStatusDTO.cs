namespace Tripzo.DTOs.Operator
{
    public class BusBookingStatusDTO
    {
        public int BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public List<ScheduleBookingStatusDTO> ScheduleBookings { get; set; } = new();
    }

    public class ScheduleBookingStatusDTO
    {
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public int TotalSeats { get; set; }
        public int BookedSeats { get; set; }
        public int AvailableSeats { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
