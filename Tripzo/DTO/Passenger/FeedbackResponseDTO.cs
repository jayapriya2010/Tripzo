namespace Tripzo.DTOs.Passenger;

public class FeedbackResponseDTO
{
    public int FeedbackId { get; set; }
    public int BookingId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
