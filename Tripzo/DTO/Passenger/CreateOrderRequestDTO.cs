using System.ComponentModel.DataAnnotations;

namespace Tripzo.DTOs.Passenger
{
    public class CreateOrderRequestDTO
    {
        [Required(ErrorMessage = "Route ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Route ID must be a positive number.")]
        public int RouteId { get; set; }

        [Required(ErrorMessage = "Bus ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Bus ID must be a positive number.")]
        public int BusId { get; set; }

        [Required(ErrorMessage = "At least one seat must be selected.")]
        [MinLength(1, ErrorMessage = "At least one seat must be selected.")]
        public List<int> SelectedSeatIds { get; set; } = [];

        [Required(ErrorMessage = "Journey date is required.")]
        public DateTime JourneyDate { get; set; }

        [Required(ErrorMessage = "Boarding stop is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Boarding stop ID must be a positive number.")]
        public int BoardingStopId { get; set; }

        [Required(ErrorMessage = "Dropping stop is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Dropping stop ID must be a positive number.")]
        public int DroppingStopId { get; set; }
    }
}
