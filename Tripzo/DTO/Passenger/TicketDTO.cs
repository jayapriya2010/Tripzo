namespace Tripzo.DTOs.Passenger;

public class TicketDTO
{
    public int BookingId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerEmail { get; set; } = string.Empty;
    public string SourceCity { get; set; } = string.Empty;
    public string DestCity { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public string BusType { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public List<string> SeatNumbers { get; set; } = [];
    public List<PassengerDetailDTO> Passengers { get; set; } = []; // New traveler details
    public decimal TotalAmount { get; set; }
    public DateTime BookingDate { get; set; }
}
