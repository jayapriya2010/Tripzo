using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int BusId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Operator Response
        public string? OperatorResponse { get; set; }
        public DateTime? RespondedAt { get; set; }

        // Navigation Properties
        public virtual Booking Booking { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual Bus Bus { get; set; } = null!;
    }
}
