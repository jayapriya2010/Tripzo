namespace Tripzo.DTOs.Operator
{
    public class ApprovedCancellationDTO
    {
        public int BookingId { get; set; }
        public string PassengerName { get; set; }
        public string PassengerEmail { get; set; }
        public string RouteName { get; set; }
        public DateTime JourneyDate { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime CancellationDate { get; set; }
    }
}
