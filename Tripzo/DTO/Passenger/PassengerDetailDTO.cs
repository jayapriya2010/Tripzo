using System.ComponentModel.DataAnnotations;

namespace Tripzo.DTOs.Passenger
{
    public class PassengerDetailDTO
    {
        [Required]
        public int SeatId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 120)]
        public int Age { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;
    }
}
