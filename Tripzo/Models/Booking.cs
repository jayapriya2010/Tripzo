using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        public int RouteId { get; set; }
        public virtual Route Route { get; set; }

        [Required]
        public int BoardingStopId { get; set; }
        [Required]
        public int DroppingStopId { get; set; }

        [Required]
        public DateTime JourneyDate { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal TotalAmount { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now; // When the ticket was bought

        [Required]
        public string Status { get; set; } // "Confirmed", "Cancelled", "CancellationApproved", "Refunded"

        public virtual ICollection<BookedSeat> BookedSeats { get; set; }
        public virtual Payment Payment { get; set; }
    }
}