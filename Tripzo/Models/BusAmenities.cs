using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class BusAmenity
    {
        [Required]
        public int BusId { get; set; }
        public virtual Bus Bus { get; set; }

        [Required]
        public int AmenityId { get; set; }
        public virtual AmenityMaster Amenity { get; set; }
    }
}
