using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class AmenityMaster
    {
        public int AmenityId { get; set; }

        [Required]
        [StringLength(100)]
        public string AmenityName { get; set; }

        public virtual ICollection<BusAmenity> BusAmenities { get; set; }
    }
}