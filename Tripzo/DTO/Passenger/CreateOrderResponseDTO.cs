namespace Tripzo.DTOs.Passenger
{
    public class CreateOrderResponseDTO
    {
        public string OrderId { get; set; } = string.Empty;     // Razorpay order_id (order_xxxxx)
        public decimal Amount { get; set; }                      // Server-calculated total in rupees
        public string Currency { get; set; } = "INR";
        public string RazorpayKeyId { get; set; } = string.Empty; // For frontend SDK initialization
    }
}
