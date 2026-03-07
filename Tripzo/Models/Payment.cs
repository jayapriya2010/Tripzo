using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; }

        [Required]
        [Range(-100000, 100000)]
        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; }
    }
}