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

        [Required(ErrorMessage = "At least one passenger detail must be provided.")]
        public List<PassengerDetailDTO> Passengers { get; set; } = [];

        [Required(ErrorMessage = "Primary email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string PrimaryEmail { get; set; } = string.Empty;

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
