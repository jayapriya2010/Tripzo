namespace Tripzo.DTOs.Operator
{
    public class RefundRequestDTO
    {
        public int BookingId { get; set; }
        public decimal RefundAmount { get; set; }
        public string RefundReason { get; set; }
        public DateTime RefundProcessedDate { get; set; }
    }
}