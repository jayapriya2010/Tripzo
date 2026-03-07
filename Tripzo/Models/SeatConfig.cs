using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class SeatConfig
    {
        public int SeatId { get; set; }

        [Required]
        public int BusId { get; set; }
        public virtual Bus Bus { get; set; }

        [Required]
        [StringLength(20)]
        public string SeatNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string SeatType { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal AddonFare { get; set; }
    }
}
