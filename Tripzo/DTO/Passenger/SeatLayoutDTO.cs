namespace Tripzo.DTOs.Passenger
{
    public class SeatLayoutDTO
    {
        public int SeatId { get; set; }
        public string SeatNumber { get; set; }
        public string SeatType { get; set; } // Sleeper, Seater
        public bool IsAvailable { get; set; } // Logic for the 'Cross Mark'
        public decimal FinalPrice { get; set; } // BaseFare + AddonFare
    }
}