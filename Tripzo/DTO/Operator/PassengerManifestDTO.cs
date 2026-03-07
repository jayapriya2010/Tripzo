namespace Tripzo.DTOs.Operator
{
    public class PassengerManifestDTO
    {
        public string SeatNumber { get; set; }
        public string PassengerName { get; set; }
        public string ContactNumber { get; set; }
        public string BoardingPoint { get; set; }
        public string DroppingPoint { get; set; }
        public string BookingStatus { get; set; }
    }
}

//This DTO allows them to see exactly which passenger is sitting in which seat for a trip.

