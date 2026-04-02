using System.ComponentModel.DataAnnotations;

namespace Tripzo.DTOs.Passenger
{
    public class CancelBookingDTO
    {
        [Required(ErrorMessage = "Booking ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Booking ID must be a positive number.")]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "User ID must be a positive number.")]
        public int UserId { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }

        public List<int>? SelectedSeatIds { get; set; }
    }
}