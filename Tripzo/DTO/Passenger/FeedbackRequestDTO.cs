using System.ComponentModel.DataAnnotations;

namespace Tripzo.DTOs.Passenger;

public class FeedbackRequestDTO
{
    [Required(ErrorMessage = "Booking ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Booking ID must be a positive number.")]
    public int BookingId { get; set; }

    [Required(ErrorMessage = "Rating is required.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }
}
