namespace Tripzo.DTOs.Passenger
{
    public class BookingResponseDTO
    {
        public int BookingId { get; set; }
        public string PNR { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
    }
}