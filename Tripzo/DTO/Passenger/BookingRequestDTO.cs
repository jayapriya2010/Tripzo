namespace Tripzo.DTOs.Passenger
{
    public class BookingRequestDTO
    {
        public int RouteId { get; set; }
        public int UserId { get; set; }
        public List<int> SelectedSeatIds { get; set; }
        public DateTime JourneyDate { get; set; }
        public int BoardingStopId { get; set; }
        public int DroppingStopId { get; set; }
        public decimal TotalPaid { get; set; }
    }
}