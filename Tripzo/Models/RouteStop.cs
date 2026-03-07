using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class RouteStop
    {
        public int StopId { get; set; }

        [Required]
        public int RouteId { get; set; }
        public virtual Route Route { get; set; }

        [Required]
        [StringLength(100)]
        public string CityName { get; set; }

        [Required]
        [StringLength(200)]
        public string LocationName { get; set; }

        [Required]
        [StringLength(50)]
        public string StopType { get; set; } // "Boarding" or "Dropping"

        [Required]
        public int StopOrder { get; set; }

        [Required]
        public TimeSpan ArrivalTime { get; set; }
    }
}