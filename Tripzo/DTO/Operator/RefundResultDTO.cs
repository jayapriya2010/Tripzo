namespace Tripzo.DTOs.Operator
{
    public class RefundResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public string? RazorpayPaymentId { get; set; } // For triggering Razorpay refund
        public string SeatNumbers { get; set; } = string.Empty;
    }
}
