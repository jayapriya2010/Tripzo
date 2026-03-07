using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class Bus
    {
        public int BusId { get; set; }

        [Required]
        [StringLength(150)]
        public string BusName { get; set; }

        [Required]
        [StringLength(50)]
        public string BusNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string BusType { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true; // Soft delete

        public int OperatorId { get; set; }
        public virtual User Operator { get; set; }

        public virtual ICollection<SeatConfig> SeatConfigs { get; set; }
        public virtual ICollection<BusAmenity> BusAmenities { get; set; }
        public virtual ICollection<Route> Routes { get; set; }
    }
}
