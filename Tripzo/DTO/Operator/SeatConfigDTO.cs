namespace Tripzo.DTOs.Operator
{
    public class SeatConfigDTO
    {
        public string SeatNumber { get; set; }
        public string SeatType { get; set; } // Window, Aisle, Lower, Upper
        public decimal AddonFare { get; set; }
    }
}