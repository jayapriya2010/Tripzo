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

        // 1. Search for Routes: Includes Bus, BusAmenities and Stops, ensuring the Bus is Active
        public async Task<List<Tripzo.Models.Route>> SearchRoutesAsync(string fromCity, string toCity, DateTime travelDate)
        {
            return await _context.Routes
                .Include(r => r.Bus)
                    .ThenInclude(b => b.BusAmenities)
                        .ThenInclude(ba => ba.Amenity)
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

        // 6. Get booking details for generating ticket PDF and sending email
        public async Task<TicketDTO?> GetBookingDetailsForTicketAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Route)
                    .ThenInclude(r => r.Bus)
                .Include(b => b.BookedSeats)
                    .ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return null;

            return new TicketDTO
            {
                BookingId = booking.BookingId,
                PassengerName = booking.User?.FullName ?? "Guest",
                PassengerEmail = booking.User?.Email ?? "",
                SourceCity = booking.Route?.SourceCity ?? "",
                DestCity = booking.Route?.DestCity ?? "",
                BusName = booking.Route?.Bus?.BusName ?? "",
                BusNumber = booking.Route?.Bus?.BusNumber ?? "",
                JourneyDate = booking.JourneyDate,
                SeatNumbers = booking.BookedSeats?.Select(bs => bs.Seat?.SeatNumber ?? "").ToList() ?? [],
                TotalAmount = booking.TotalAmount,
                BookingDate = booking.BookingDate
            };
        }

        // 7. Submit feedback for a completed journey
        public async Task<FeedbackResponseDTO?> SubmitFeedbackAsync(int userId, FeedbackRequestDTO request)
        {
            // Verify booking exists and belongs to user
            var booking = await _context.Bookings
                .Include(b => b.Route)
                    .ThenInclude(r => r.Bus)
                .FirstOrDefaultAsync(b => b.BookingId == request.BookingId && b.UserId == userId);

            if (booking == null) return null;

            // Verify journey is completed (journey date has passed)
            if (booking.JourneyDate.Date > DateTime.Today)
                return null;

            // Check if feedback already exists
            var existingFeedback = await _context.Feedbacks
                .AnyAsync(f => f.BookingId == request.BookingId);

            if (existingFeedback) return null;

            var feedback = new Feedback
            {
                BookingId = request.BookingId,
                UserId = userId,
                BusId = booking.Route.BusId, // Add BusId from the booking's route
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return new FeedbackResponseDTO
            {
                FeedbackId = feedback.FeedbackId,
                BookingId = feedback.BookingId,
                RouteName = $"{booking.Route?.SourceCity} to {booking.Route?.DestCity}",
                BusName = booking.Route?.Bus?.BusName ?? "",
                JourneyDate = booking.JourneyDate,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                CreatedAt = feedback.CreatedAt
            };
        }

        // 8. Get all feedbacks submitted by a user
        public async Task<List<FeedbackResponseDTO>> GetUserFeedbacksAsync(int userId)
        {
            return await _context.Feedbacks
                .Include(f => f.Booking)
                    .ThenInclude(b => b.Route)
                        .ThenInclude(r => r.Bus)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackResponseDTO
                {
                    FeedbackId = f.FeedbackId,
                    BookingId = f.BookingId,
                    RouteName = $"{f.Booking.Route.SourceCity} to {f.Booking.Route.DestCity}",
                    BusName = f.Booking.Route.Bus.BusName,
                    JourneyDate = f.Booking.JourneyDate,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        // 9. Check if user has completed the journey (journey date has passed)
        public async Task<bool> HasUserCompletedJourneyAsync(int userId, int bookingId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

            if (booking == null) return false;

            // Allow feedback for confirmed bookings where journey date has passed
            // Also allow for completed journeys (not cancelled/refunded)
            var validStatuses = new[] { "Confirmed", "Completed" };
            if (!validStatuses.Contains(booking.Status)) return false;

            return booking.JourneyDate.Date <= DateTime.Today;
        }

        // 10. Check if feedback already exists for a booking
        public async Task<bool> HasFeedbackAsync(int bookingId)
        {
            return await _context.Feedbacks.AnyAsync(f => f.BookingId == bookingId);
        }

        // 11. Get feedback for a specific bus
        public async Task<BusFeedbackSummaryDTO?> GetBusFeedbackAsync(int busId)
        {
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null) return null;

            var feedbacks = await _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Booking)
                .Where(f => f.BusId == busId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var ratingGroups = feedbacks.GroupBy(f => f.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            return new BusFeedbackSummaryDTO
            {
                BusId = bus.BusId,
                BusName = bus.BusName,
                BusNumber = bus.BusNumber,
                AverageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 0,
                TotalReviews = feedbacks.Count,
                FiveStarCount = ratingGroups.GetValueOrDefault(5, 0),
                FourStarCount = ratingGroups.GetValueOrDefault(4, 0),
                ThreeStarCount = ratingGroups.GetValueOrDefault(3, 0),
                TwoStarCount = ratingGroups.GetValueOrDefault(2, 0),
                OneStarCount = ratingGroups.GetValueOrDefault(1, 0),
                Reviews = feedbacks.Select(f => new BusFeedbackDTO
                {
                    FeedbackId = f.FeedbackId,
                    PassengerName = f.User?.FullName ?? "Anonymous",
                    Rating = f.Rating,
                    Comment = f.Comment,
                    JourneyDate = f.Booking?.JourneyDate ?? DateTime.MinValue,
                    CreatedAt = f.CreatedAt,
                    OperatorResponse = f.OperatorResponse,
                    RespondedAt = f.RespondedAt
                }).ToList()
            };
        }

        // 12. Get average rating for a bus
        public async Task<(double? averageRating, int totalReviews)> GetBusRatingAsync(int busId)
        {
            var feedbacks = await _context.Feedbacks
                .Where(f => f.BusId == busId)
                .ToListAsync();

            if (!feedbacks.Any())
                return (null, 0);

            return (Math.Round(feedbacks.Average(f => f.Rating), 1), feedbacks.Count);
        }
    }
}