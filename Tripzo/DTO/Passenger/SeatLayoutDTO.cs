namespace Tripzo.DTOs.Passenger
{
    public class SeatLayoutDTO
    {
        public int SeatId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatType { get; set; } = string.Empty; // Berth|Position|Category
        public string Berth { get; set; } = string.Empty; // Lower, Upper
        public string Position { get; set; } = string.Empty; // Window, Aisle
        public string Category { get; set; } = string.Empty; // Sleeper, Seater
        public bool IsAvailable { get; set; } // Logic for the 'Cross Mark'
        public decimal FinalPrice { get; set; } // BaseFare + AddonFare
    }
}