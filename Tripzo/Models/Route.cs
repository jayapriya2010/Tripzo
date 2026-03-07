using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tripzo.Models
{
    public class Route
    {
        public int RouteId { get; set; }

        [Required]
        public int BusId { get; set; }

        [ForeignKey("BusId")]
        public virtual Bus Bus { get; set; }

        [Required]
        [StringLength(100)]
        public string SourceCity { get; set; }

        [Required]
        [StringLength(100)]
        public string DestCity { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal BaseFare { get; set; }

        public virtual ICollection<RouteStop> RouteStops { get; set; }
    }
}