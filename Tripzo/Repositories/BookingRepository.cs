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

        // 1. Search for Scheduled Routes: Returns the actual bus scheduled for that specific date
        
        public async Task<List<ScheduledRouteDTO>> SearchScheduledRoutesAsync(string fromCity, string toCity, DateTime travelDate)
        {
            var results = await _context.BusSchedules
                .Include(bs => bs.Bus)
                    .ThenInclude(b => b.BusAmenities)
                        .ThenInclude(ba => ba.Amenity)
                .Include(bs => bs.Bus)
                    .ThenInclude(b => b.SeatConfigs)
                .Include(bs => bs.Route)
                    .ThenInclude(r => r.RouteStops)
                .Where(bs => bs.Route.SourceCity.ToLower() == fromCity.ToLower() &&
                            bs.Route.DestCity.ToLower() == toCity.ToLower() &&
                            bs.ScheduledDate.Date == travelDate.Date &&
                            bs.IsActive)
                .ToListAsync();

            return results.Select(bs => {
                var stops = bs.Route.RouteStops.OrderBy(s => s.StopOrder).ToList();
                var sourceStop = stops.FirstOrDefault();
                var destStop = stops.LastOrDefault();

                var departure = bs.ScheduledDate.Date;
                var arrival = bs.ScheduledDate.Date;

                if (sourceStop != null)
                {
                    departure = bs.ScheduledDate.Date.AddDays(sourceStop.DayOffset).Add(sourceStop.ArrivalTime);
                }
                
                if (destStop != null)
                {
                    arrival = bs.ScheduledDate.Date.AddDays(destStop.DayOffset).Add(destStop.ArrivalTime);
                }

                return new ScheduledRouteDTO
                {
                    ScheduleId = bs.ScheduleId,
                    RouteId = bs.RouteId,
                    BusId = bs.BusId,
                    Bus = bs.Bus,
                    Route = bs.Route,
                    ScheduledDate = bs.ScheduledDate,
                    DepartureDateTime = departure,
                    ArrivalDateTime = arrival,
                    HasNextDayArrival = (destStop?.DayOffset ?? 0) > (sourceStop?.DayOffset ?? 0)
                };
            }).ToList();
        }

        // Legacy search method - kept for compatibility
        // Note: Only checks BusSchedule.IsActive, not Bus.IsActive (bus deactivation is for future scheduling only)
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
                               bs.IsActive)) // Only check schedule is active
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

        // 2b. Calculate total fare server-side from selected seats
        public async Task<decimal> CalculateTotalFareAsync(int routeId, List<PassengerDetailDTO> passengers)
        {
            var seatIds = passengers.Select(p => p.SeatId).ToList();
            var route = await _context.Routes.FirstOrDefaultAsync(r => r.RouteId == routeId);
            if (route == null) return 0;
 
            var seats = await _context.SeatConfigs
                .Where(s => seatIds.Contains(s.SeatId))
                .ToListAsync();
 
            // Total = sum of (BaseFare + AddonFare) for each selected seat
            return seats.Sum(s => route.BaseFare + s.AddonFare);
        }

        // 3. Create a Booking: Uses a Database Transaction for Atomic Group Booking
        // busId is the scheduled bus for that specific date (may differ from route's default bus)
        public async Task<Booking> CreateBookingAsync(Booking booking, int busId, List<PassengerDetailDTO> passengers)
        {
            var seatIds = passengers.Select(p => p.SeatId).ToList();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Verify user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == booking.UserId);
                if (!userExists)
                    throw new Exception($"User {booking.UserId} not found");

                // 2. Verify route exists
                var route = await _context.Routes
                    .FirstOrDefaultAsync(r => r.RouteId == booking.RouteId);
                if (route == null)
                    throw new Exception($"Route {booking.RouteId} not found");

                // 3. Verify there's an active schedule for this route + bus + date combination
                var activeSchedule = await _context.BusSchedules
                    .Include(bs => bs.Bus)
                    .FirstOrDefaultAsync(bs => bs.RouteId == booking.RouteId &&
                                              bs.BusId == busId &&
                                              bs.ScheduledDate.Date == booking.JourneyDate.Date &&
                                              bs.IsActive);
                if (activeSchedule == null)
                    throw new Exception($"No active schedule found for this route and bus on {booking.JourneyDate:yyyy-MM-dd}");

                // 4. Verify boarding stop exists and is of type "Boarding"
                var boardingStop = await _context.RouteStops
                    .FirstOrDefaultAsync(rs => rs.RouteId == booking.RouteId && 
                                              rs.StopId == booking.BoardingStopId);

                if (boardingStop == null)
                    throw new Exception($"Boarding stop {booking.BoardingStopId} is not valid for this route.");

                if (boardingStop.StopType != "Boarding")
                    throw new Exception($"Stop '{boardingStop.LocationName}' (ID: {booking.BoardingStopId}) is not a boarding point. It is a '{boardingStop.StopType}' stop.");

                // 5. Verify dropping stop exists and is of type "Dropping"
                var droppingStop = await _context.RouteStops
                    .FirstOrDefaultAsync(rs => rs.RouteId == booking.RouteId && 
                                              rs.StopId == booking.DroppingStopId);

                if (droppingStop == null)
                    throw new Exception($"Dropping stop {booking.DroppingStopId} is not valid for this route.");

                if (droppingStop.StopType != "Dropping")
                    throw new Exception($"Stop '{droppingStop.LocationName}' (ID: {booking.DroppingStopId}) is not a dropping point. It is a '{droppingStop.StopType}' stop.");

                // 6. Verify boarding stop comes before dropping stop in the route order
                if (boardingStop.StopOrder >= droppingStop.StopOrder)
                    throw new Exception($"Boarding stop '{boardingStop.LocationName}' (Order: {boardingStop.StopOrder}) must come before dropping stop '{droppingStop.LocationName}' (Order: {droppingStop.StopOrder}).");

                // 7. Verify all seats exist and belong to the SCHEDULED bus (not route's default bus)
                var busSeats = await _context.SeatConfigs
                    .Where(s => s.BusId == busId && seatIds.Contains(s.SeatId))
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
                    throw new Exception($"Seats {string.Join(", ", invalidSeats)} do not belong to bus {activeSchedule.Bus.BusName} (BusId: {busId}). {debugInfo}");
                }

                // 8. Verify all seats are available before proceeding
                // Check against the specific bus's seats for this route and date
                var occupiedSeatIds = await _context.BookedSeats
                    .Where(bs => bs.Booking.RouteId == booking.RouteId &&
                                 bs.Booking.JourneyDate.Date == booking.JourneyDate.Date &&
                                 bs.Booking.Status == "Confirmed" &&
                                 bs.Seat.BusId == busId &&
                                 seatIds.Contains(bs.SeatId))
                    .Select(bs => bs.SeatId)
                    .ToListAsync();

                if (occupiedSeatIds.Any())
                {
                    throw new Exception($"Seats {string.Join(", ", occupiedSeatIds)} are already booked");
                }

                // 9. Save the main Booking record
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // 10. Link the selected seats to this booking with passenger info
                foreach (var pass in passengers)
                {
                    _context.BookedSeats.Add(new BookedSeat
                    {
                        BookingId = booking.BookingId,
                        SeatId = pass.SeatId,
                        PassengerName = pass.Name,
                        PassengerAge = pass.Age,
                        Gender = pass.Gender,
                        PassengerPhone = pass.Phone
                    });
                }

                // 11. ATOMIC PAYMENT: Create the payment record immediately
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

        // 3b. Create a Booking with Razorpay payment details
        public async Task<Booking> CreateBookingWithRazorpayAsync(Booking booking, int busId, List<PassengerDetailDTO> passengers, string razorpayOrderId, string razorpayPaymentId)
        {
            var seatIds = passengers.Select(p => p.SeatId).ToList();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Verify user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == booking.UserId);
                if (!userExists)
                    throw new Exception($"User {booking.UserId} not found");

                // 2. Verify route exists
                var route = await _context.Routes
                    .FirstOrDefaultAsync(r => r.RouteId == booking.RouteId);
                if (route == null)
                    throw new Exception($"Route {booking.RouteId} not found");

                // 3. Verify there's an active schedule for this route + bus + date
                var activeSchedule = await _context.BusSchedules
                    .Include(bs => bs.Bus)
                    .FirstOrDefaultAsync(bs => bs.RouteId == booking.RouteId &&
                                              bs.BusId == busId &&
                                              bs.ScheduledDate.Date == booking.JourneyDate.Date &&
                                              bs.IsActive);
                if (activeSchedule == null)
                    throw new Exception($"No active schedule found for this route and bus on {booking.JourneyDate:yyyy-MM-dd}");

                // 4. Verify boarding stop
                var boardingStop = await _context.RouteStops
                    .FirstOrDefaultAsync(rs => rs.RouteId == booking.RouteId && rs.StopId == booking.BoardingStopId);
                if (boardingStop == null)
                    throw new Exception($"Boarding stop {booking.BoardingStopId} is not valid for this route.");
                if (boardingStop.StopType != "Boarding")
                    throw new Exception($"Stop '{boardingStop.LocationName}' (ID: {booking.BoardingStopId}) is not a boarding point.");

                // 5. Verify dropping stop
                var droppingStop = await _context.RouteStops
                    .FirstOrDefaultAsync(rs => rs.RouteId == booking.RouteId && rs.StopId == booking.DroppingStopId);
                if (droppingStop == null)
                    throw new Exception($"Dropping stop {booking.DroppingStopId} is not valid for this route.");
                if (droppingStop.StopType != "Dropping")
                    throw new Exception($"Stop '{droppingStop.LocationName}' (ID: {booking.DroppingStopId}) is not a dropping point.");

                // 6. Verify stop order
                if (boardingStop.StopOrder >= droppingStop.StopOrder)
                    throw new Exception($"Boarding stop must come before dropping stop.");

                // 7. Verify seats belong to the scheduled bus
                var busSeats = await _context.SeatConfigs
                    .Where(s => s.BusId == busId && seatIds.Contains(s.SeatId))
                    .Select(s => s.SeatId)
                    .ToListAsync();

                var invalidSeats = seatIds.Except(busSeats).ToList();
                if (invalidSeats.Any())
                    throw new Exception($"Seats {string.Join(", ", invalidSeats)} do not belong to bus {activeSchedule.Bus.BusName} (BusId: {busId}).");

                // 8. Verify seats are available
                var occupiedSeatIds = await _context.BookedSeats
                    .Where(bs => bs.Booking.RouteId == booking.RouteId &&
                                 bs.Booking.JourneyDate.Date == booking.JourneyDate.Date &&
                                 bs.Booking.Status == "Confirmed" &&
                                 bs.Seat.BusId == busId &&
                                 seatIds.Contains(bs.SeatId))
                    .Select(bs => bs.SeatId)
                    .ToListAsync();

                if (occupiedSeatIds.Any())
                    throw new Exception($"Seats {string.Join(", ", occupiedSeatIds)} are already booked");

                // 9. Save the booking
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // 10. Link seats to booking with passenger info
                foreach (var pass in passengers)
                {
                    _context.BookedSeats.Add(new BookedSeat
                    {
                        BookingId = booking.BookingId,
                        SeatId = pass.SeatId,
                        PassengerName = pass.Name,
                        PassengerAge = pass.Age,
                        Gender = pass.Gender,
                        PassengerPhone = pass.Phone
                    });
                }

                // 11. Create payment record with Razorpay details
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    TransactionId = razorpayPaymentId, // Use Razorpay payment ID as transaction ID
                    AmountPaid = booking.TotalAmount,
                    PaymentStatus = "Completed",
                    PaymentDate = DateTime.Now,
                    RazorpayOrderId = razorpayOrderId,
                    RazorpayPaymentId = razorpayPaymentId
                };
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return booking;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMessage = ex.Message;
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    errorMessage += $" | Inner: {innerEx.Message}";
                    innerEx = innerEx.InnerException;
                }
                throw new ApplicationException($"Booking failed: {errorMessage}");
            }
        }

        // 4. Retrieve personal journey history for a specific User
        public async Task<IEnumerable<Booking>> GetPassengerHistoryAsync(int userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Route)
                    .ThenInclude(r => r.Bus)
                .Include(b => b.BookedSeats)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.JourneyDate)
                .ToListAsync();

            // Resolve actual scheduled buses for each history item
            foreach (var b in bookings)
            {
                var schedule = await _context.BusSchedules
                    .Include(bs => bs.Bus)
                    .FirstOrDefaultAsync(bs => bs.RouteId == b.RouteId && 
                                              bs.ScheduledDate.Date == b.JourneyDate.Date && 
                                              bs.IsActive);
                
                if (schedule != null && b.Route != null)
                {
                    // CRITICAL FIX: create a SHADOW route object for this instance
                    // This prevents multiple bookings sharing the same Route reference from 
                    // overwriting each other's bus details in the history list.
                    b.Route = new Models.Route
                    {
                        RouteId = b.Route.RouteId,
                        BusId = schedule.BusId,
                        Bus = schedule.Bus, // The reassigned/scheduled bus
                        SourceCity = b.Route.SourceCity,
                        DestCity = b.Route.DestCity,
                        BaseFare = b.Route.BaseFare
                    };
                }
            }

            return bookings;
        }

        // 5. Cancel a booking: Updates status and returns details for email notification
        public async Task<CancellationResultDTO> CancelBookingAsync(int bookingId, int userId, string? reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Verify the booking belongs to the user and is currently Confirmed
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Route)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

                if (booking == null || booking.Status != "Confirmed")
                {
                    return new CancellationResultDTO
                    {
                        Success = false,
                        Message = "Booking not found, already cancelled, or does not belong to you."
                    };
                }

                // 2. Update status and save reason
                booking.Status = "Cancelled";
                booking.CancellationReason = reason;

                // 3. Release the seats so they appear available in Search
                var bookedSeats = _context.BookedSeats.Where(bs => bs.BookingId == bookingId);
                _context.BookedSeats.RemoveRange(bookedSeats);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new CancellationResultDTO
                {
                    Success = true,
                    Message = "Ticket cancelled successfully. Your refund request has been sent for review.",
                    BookingId = booking.BookingId,
                    PassengerName = booking.User?.FullName ?? "Passenger",
                    PassengerEmail = booking.User?.Email ?? "",
                    RouteName = $"{booking.Route?.SourceCity} to {booking.Route?.DestCity}",
                    JourneyDate = booking.JourneyDate,
                    Amount = booking.TotalAmount
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new CancellationResultDTO
                {
                    Success = false,
                    Message = "An error occurred while processing your cancellation request."
                };
            }
        }

        // 6. Get booking details for generating ticket PDF and sending email
        public async Task<TicketDTO?> GetBookingDetailsForTicketAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Route)
                    .ThenInclude(r => r.Bus)
                .Include(b => b.BoardingStop)
                .Include(b => b.DroppingStop)
                .Include(b => b.BookedSeats)
                    .ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return null;

            // Resolve the ACTUAL bus from the schedule for this specific date
            var schedule = await _context.BusSchedules
                .Include(bs => bs.Bus)
                .FirstOrDefaultAsync(bs => bs.RouteId == booking.RouteId && 
                                          bs.ScheduledDate.Date == booking.JourneyDate.Date && 
                                          bs.IsActive);

            var effectiveBus = schedule?.Bus ?? booking.Route?.Bus;

            return new TicketDTO
            {
                BookingId = booking.BookingId,
                PassengerName = booking.User?.FullName ?? "Guest",
                PassengerEmail = booking.User?.Email ?? "",
                SourceCity = booking.Route?.SourceCity ?? "",
                DestCity = booking.Route?.DestCity ?? "",
                BusName = effectiveBus?.BusName ?? "",
                BusNumber = effectiveBus?.BusNumber ?? "",
                BusType = effectiveBus?.BusType ?? "",
                JourneyDate = booking.JourneyDate,
                DepartureDateTime = booking.JourneyDate.Date
                    .AddDays(booking.BoardingStop?.DayOffset ?? 0)
                    .Add(booking.BoardingStop?.ArrivalTime ?? TimeSpan.Zero),
                ArrivalDateTime = booking.JourneyDate.Date
                    .AddDays(booking.DroppingStop?.DayOffset ?? 0)
                    .Add(booking.DroppingStop?.ArrivalTime ?? TimeSpan.Zero),
                SeatNumbers = booking.BookedSeats?.Select(bs => bs.Seat?.SeatNumber ?? "").ToList() ?? [],
                TotalAmount = booking.TotalAmount,
                BookingDate = booking.BookingDate,
                Passengers = booking.BookedSeats?.Select(bs => new PassengerDetailDTO
                {
                    SeatId = bs.SeatId,
                    Name = bs.PassengerName,
                    Age = bs.PassengerAge,
                    Gender = bs.Gender,
                    Phone = bs.PassengerPhone
                }).ToList() ?? []
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

            // Resolve the ACTUAL bus for this feedback
            var schedule = await _context.BusSchedules
                .FirstOrDefaultAsync(bs => bs.RouteId == booking.RouteId && 
                                          bs.ScheduledDate.Date == booking.JourneyDate.Date && 
                                          bs.IsActive);

            var busId = schedule?.BusId ?? booking.Route.BusId;

            var feedback = new Feedback
            {
                BookingId = request.BookingId,
                UserId = userId,
                BusId = busId,
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
                CreatedAt = feedback.CreatedAt,
                OperatorResponse = feedback.OperatorResponse,
                RespondedAt = feedback.RespondedAt
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
                    CreatedAt = f.CreatedAt,
                    OperatorResponse = f.OperatorResponse,
                    RespondedAt = f.RespondedAt
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