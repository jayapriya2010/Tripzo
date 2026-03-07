using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.Models;
using Tripzo.DTOs.Passenger;


namespace Tripzo.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        // 1. Search for Routes: Includes oBus and Stops, ensuring the Bus is Active
        public async Task<List<Tripzo.Models.Route>> SearchRoutesAsync(string fromCity, string toCity, DateTime travelDate)
        {
            return await _context.Routes
                .Include(r => r.Bus)
                .Include(r => r.RouteStops)
                .Where(r => r.SourceCity.ToLower() == fromCity.ToLower() &&
                           r.DestCity.ToLower() == toCity.ToLower() &&
                           _context.BusSchedules.Any(bs => 
                               bs.RouteId == r.RouteId && 
                               bs.ScheduledDate.Date == travelDate.Date &&
                               bs.IsActive))
                .ToListAsync();
        }
        // 2. Real-time Seat Layout: Combines physical layout with current bookings
        // This consolidates getting all seats and identifying occupied ones for the UI
        // Inside BookingRepository.cs
        public async Task<List<SeatLayoutDTO>> GetSeatLayoutAsync(int busId, int routeId, DateTime travelDate)
        {
            var allSeats = await _context.SeatConfigs
                .Where(s => s.BusId == busId)
                .ToListAsync();

            var occupiedSeatIds = await _context.BookedSeats
                .Where(bs => bs.Booking.RouteId == routeId &&
                             bs.Booking.JourneyDate.Date == travelDate.Date &&
                             bs.Booking.Status == "Confirmed")
                .Select(bs => bs.SeatId)
                .ToListAsync();

            return allSeats.Select(s => new SeatLayoutDTO
            {
                SeatId = s.SeatId,
                SeatNumber = s.SeatNumber,
                SeatType = s.SeatType, // Ensure this property is mapped!
                IsAvailable = !occupiedSeatIds.Contains(s.SeatId),
                FinalPrice = _context.Routes.FirstOrDefault(r => r.RouteId == routeId).BaseFare + s.AddonFare
            }).ToList();
        }

        // Get count of available seats for search results
        public async Task<int> GetAvailableSeatsCountAsync(int busId, int routeId, DateTime travelDate)
        {
            // Get total seats in the bus
            var totalSeats = await _context.SeatConfigs
                .Where(s => s.BusId == busId)
                .CountAsync();

            // Get booked seats for this route and date
            var bookedSeatsCount = await _context.BookedSeats
                .Where(bs => bs.Booking.RouteId == routeId &&
                             bs.Booking.JourneyDate.Date == travelDate.Date &&
                             bs.Booking.Status == "Confirmed")
                .CountAsync();

            return totalSeats - bookedSeatsCount;
        }

        // 3. Create a Booking: Uses a Database Transaction for Atomic Group Booking
        public async Task<Booking> CreateBookingAsync(Booking booking, List<int> seatIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Verify user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == booking.UserId);
                if (!userExists)
                    throw new Exception($"User {booking.UserId} not found");

                // 2. Verify route exists
                var route = await _context.Routes
                    .Include(r => r.Bus)
                    .FirstOrDefaultAsync(r => r.RouteId == booking.RouteId);
                if (route == null)
                    throw new Exception($"Route {booking.RouteId} not found");

                // 3. Verify boarding and dropping stops exist for this route
                var validStops = await _context.RouteStops
                    .Where(rs => rs.RouteId == booking.RouteId &&
                                (rs.StopId == booking.BoardingStopId || rs.StopId == booking.DroppingStopId))
                    .Select(rs => rs.StopId)
                    .ToListAsync();

                if (!validStops.Contains(booking.BoardingStopId))
                    throw new Exception($"Boarding stop {booking.BoardingStopId} is not valid for this route");
                if (!validStops.Contains(booking.DroppingStopId))
                    throw new Exception($"Dropping stop {booking.DroppingStopId} is not valid for this route");

                // 4. Verify all seats exist and belong to the bus
                var busSeats = await _context.SeatConfigs
                    .Where(s => s.BusId == route.BusId && seatIds.Contains(s.SeatId))
                    .Select(s => s.SeatId)
                    .ToListAsync();

                var invalidSeats = seatIds.Except(busSeats).ToList();
                if (invalidSeats.Any())
                {
                    // Get the actual bus IDs for these seats to help with debugging
                    var actualBusIds = await _context.SeatConfigs
                        .Where(s => invalidSeats.Contains(s.SeatId))
                        .Select(s => new { s.SeatId, s.BusId })
                        .ToListAsync();
                    
                    var debugInfo = string.Join(", ", actualBusIds.Select(x => $"Seat {x.SeatId} is on Bus {x.BusId}"));
                    throw new Exception($"Seats {string.Join(", ", invalidSeats)} do not belong to bus {route.Bus.BusName} (BusId: {route.BusId}). {debugInfo}");
                }

                // 5. Verify all seats are available before proceeding
                var occupiedSeatIds = await _context.BookedSeats
                    .Where(bs => bs.Booking.RouteId == booking.RouteId &&
                                 bs.Booking.JourneyDate.Date == booking.JourneyDate.Date &&
                                 bs.Booking.Status == "Confirmed" &&
                                 seatIds.Contains(bs.SeatId))
                    .Select(bs => bs.SeatId)
                    .ToListAsync();

                if (occupiedSeatIds.Any())
                {
                    throw new Exception($"Seats {string.Join(", ", occupiedSeatIds)} are already booked");
                }

                // 6. Save the main Booking record
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // 7. Link the selected seats to this booking
                foreach (var seatId in seatIds)
                {
                    _context.BookedSeats.Add(new BookedSeat
                    {
                        BookingId = booking.BookingId,
                        SeatId = seatId
                    });
                }

                // 8. ATOMIC PAYMENT: Create the payment record immediately
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    TransactionId = $"TXN{DateTime.Now:yyyyMMddHHmmss}{booking.BookingId}",
                    AmountPaid = booking.TotalAmount,
                    PaymentStatus = "Completed",
                    PaymentDate = DateTime.Now
                };
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Everything saves together

                return booking;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // If anything fails, nothing is saved
                
                // Get the most detailed error message
                var errorMessage = ex.Message;
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    errorMessage += $" | Inner: {innerEx.Message}";
                    innerEx = innerEx.InnerException;
                }
                
                // Re-throw the exception with full details
                throw new ApplicationException($"Booking failed: {errorMessage}");
            }
        }

        // 4. Retrieve personal journey history for a specific User
        public async Task<IEnumerable<Booking>> GetPassengerHistoryAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Route)
                    .ThenInclude(r => r.Bus)
                .Include(b => b.BookedSeats)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.JourneyDate)
                .ToListAsync();
        }

        // 5. Cancel a booking: Updates status and includes placeholder for refund logic
        public async Task<bool> CancelBookingAsync(int bookingId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Verify the booking belongs to the user and is currently Confirmed
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

                if (booking == null || booking.Status != "Confirmed") return false;

                // 2. Update status to 'Cancelled'
                // This triggers the visibility for the Operator to see a refund is needed
                booking.Status = "Cancelled";

                // 3. Release the seats so they appear available in Search
                var bookedSeats = _context.BookedSeats.Where(bs => bs.BookingId == bookingId);
                _context.BookedSeats.RemoveRange(bookedSeats);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}