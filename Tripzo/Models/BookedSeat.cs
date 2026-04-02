using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class BookedSeat
    {
        public int BookedSeatId { get; set; }
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        public int SeatId { get; set; }
        public virtual SeatConfig Seat { get; set; }

        [Required]
        [StringLength(150)]
        public string PassengerName { get; set; } = string.Empty;

        [Range(1, 120)]
        public int PassengerAge { get; set; }

        [StringLength(20)]
        public string Gender { get; set; } = string.Empty;

        [StringLength(15)]
        public string PassengerPhone { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Confirmed";

        [StringLength(500)]
        public string? CancellationReason { get; set; }
    }
}
