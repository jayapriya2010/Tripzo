using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class BookedSeat
    {
        public int BookedSeatId { get; set; }
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        public int SeatId { get; set; }
        public virtual SeatConfig Seat { get; set; }
    }
}
