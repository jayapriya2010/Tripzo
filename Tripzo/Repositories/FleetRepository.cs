using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.DTOs.Operator;
using Tripzo.Models;
using Tripzo.DTO.Admin;

namespace Tripzo.Repositories
{
    public class FleetRepository : IFleetRepository
    {
        private readonly AppDbContext _context;

        public FleetRepository(AppDbContext context)
        {
            _context = context;
        }

        // 1. Add a new Bus to the system
        public async Task<int?> AddBusAsync(Bus bus)
        {
            // Check if bus number already exists
            var exists = await _context.Buses.AnyAsync(b => b.BusNumber == bus.BusNumber);
            if (exists)
                return null;

            _context.Buses.Add(bus);
            var saved = await _context.SaveChangesAsync() > 0;
            return saved ? bus.BusId : null;
        }

        // 2. Fetch all buses for a specific Operator (including amenities)
        public async Task<IEnumerable<Bus>> GetOperatorFleetAsync(int operatorId)
        {
            return await _context.Buses
                .Include(b => b.SeatConfigs)
                .Include(b => b.BusAmenities)
                    .ThenInclude(ba => ba.Amenity)
                .Where(b => b.OperatorId == operatorId)
                .ToListAsync();
        }

        // 3. Toggle Bus Status (Soft Delete Logic)
        public async Task<bool> UpdateBusStatusAsync(int busId, bool status)
        {
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null) return false;

            bus.IsActive = status; // true to activate, false to deactivate
            return await _context.SaveChangesAsync() > 0;
        }

        // 4. Add amenities to a bus
        public async Task<bool> AddAmenitiesToBusAsync(int busId, List<int> amenityIds)
        {
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null) return false;

            // Get existing amenity IDs for this bus
            var existingAmenityIds = await _context.BusAmenities
                .Where(ba => ba.BusId == busId)
                .Select(ba => ba.AmenityId)
                .ToListAsync();

            // Only add amenities that don't already exist
            var newAmenities = amenityIds
                .Where(id => !existingAmenityIds.Contains(id))
                .Select(amenityId => new BusAmenity
                {
                    BusId = busId,
                    AmenityId = amenityId
                }).ToList();

            if (newAmenities.Any())
            {
                _context.BusAmenities.AddRange(newAmenities);
                return await _context.SaveChangesAsync() > 0;
            }

            return true; // No new amenities to add, but not a failure
        }

        // 5. Remove amenities from a bus
        public async Task<bool> RemoveAmenitiesFromBusAsync(int busId, List<int> amenityIds)
        {
            var amenitiesToRemove = await _context.BusAmenities
                .Where(ba => ba.BusId == busId && amenityIds.Contains(ba.AmenityId))
                .ToListAsync();

            if (amenitiesToRemove.Any())
            {
                _context.BusAmenities.RemoveRange(amenitiesToRemove);
                return await _context.SaveChangesAsync() > 0;
            }

            return true;
        }

        // 6. Get all available amenities
        public async Task<IEnumerable<AmenityMaster>> GetAllAmenitiesAsync()
        {
            return await _context.Amenities.ToListAsync();
        }

        // 7. Get amenities for a specific bus
        public async Task<IEnumerable<AmenityMaster>> GetBusAmenitiesAsync(int busId)
        {
            return await _context.BusAmenities
                .Where(ba => ba.BusId == busId)
                .Include(ba => ba.Amenity)
                .Select(ba => ba.Amenity)
                .ToListAsync();
        }

        // 8. Define the physical seat map for a bus
        public async Task<SeatConfigResult> ConfigureBusSeatsAsync(int busId, List<SeatConfig> seats)
        {
            // Validate bus exists
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null)
                return SeatConfigResult.Fail($"Bus with ID {busId} not found.");

            // Check for duplicate seat numbers within the new seats being added
            var duplicatesInNewSeats = seats
                .GroupBy(s => s.SeatNumber)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatesInNewSeats.Any())
                return SeatConfigResult.DuplicateSeats(duplicatesInNewSeats);

            // Get existing seat numbers for this bus
            var existingSeatNumbers = await _context.SeatConfigs
                .Where(s => s.BusId == busId)
                .Select(s => s.SeatNumber)
                .ToListAsync();

            // Check if any new seat numbers already exist for this bus
            var conflictingSeatNumbers = seats
                .Where(s => existingSeatNumbers.Contains(s.SeatNumber))
                .Select(s => s.SeatNumber)
                .ToList();

            if (conflictingSeatNumbers.Any())
                return SeatConfigResult.SeatsAlreadyExist(conflictingSeatNumbers);

            // Count existing seats for this bus
            var existingCount = existingSeatNumbers.Count;
            var newSeatsCount = seats?.Count ?? 0;

            // Enforce capacity: existing + new must not exceed bus capacity
            if (existingCount + newSeatsCount > bus.Capacity)
                return SeatConfigResult.Fail($"Capacity exceeded. Bus capacity is {bus.Capacity}, existing seats: {existingCount}, trying to add: {newSeatsCount}.");

            // Ensure all seats are linked to the correct bus
            foreach (var seat in seats)
            {
                seat.BusId = busId;
            }

            _context.SeatConfigs.AddRange(seats);
            var saved = await _context.SaveChangesAsync() > 0;

            return saved ? SeatConfigResult.Ok() : SeatConfigResult.Fail("Failed to save seat configuration.");
        }

        // 9. Create a Route and its Stops in one transaction
        public async Task<bool> DefineRouteWithStopsAsync(Tripzo.Models.Route route, List<RouteStop> stops, DateTime? scheduleDate = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Save the main Route
                _context.Routes.Add(route);
                await _context.SaveChangesAsync(); // Generates RouteId

                // 2. Link and save all stops for this journey
                foreach (var stop in stops)
                {
                    stop.RouteId = route.RouteId;
                    _context.RouteStops.Add(stop);
                }
                await _context.SaveChangesAsync();

                // 3. Auto-Schedule if requested
                if (scheduleDate.HasValue)
                {
                    int maxOffset = stops.Any() ? stops.Max(rs => rs.DayOffset) : 0;
                    
                    // Re-use logic for checking availability
                    var availability = await CheckBusAvailabilityAsync(route.BusId, route.RouteId, maxOffset, new List<DateTime> { scheduleDate.Value });
                    
                    if (!availability.Success)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception(availability.Message); // Caught by the try-catch block
                    }

                    var schedule = new BusSchedule
                    {
                        RouteId = route.RouteId,
                        BusId = route.BusId,
                        ScheduledDate = scheduleDate.Value.Date,
                        IsActive = true
                    };
                    _context.BusSchedules.Add(schedule);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // We should ideally bubble up the specific error message, but keeping API signature for now
                // Logs or similar could go here
                return false;
            }
        }

        // 10. View all routes currently assigned to a bus
        public async Task<IEnumerable<Tripzo.Models.Route>> GetBusRoutesAsync(int busId)
        {
            return await _context.Routes
                .Include(r => r.RouteStops)
                .Where(r => r.BusId == busId)
                .ToListAsync();
        }

        // 11. Get approved cancellations for operator's buses
        public async Task<IEnumerable<Booking>> GetApprovedCancellationsForOperatorAsync(int operatorId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Route)
                    .ThenInclude(r => r.Bus)
                .Where(b => b.Status == "CancellationApproved" && b.Route.Bus.OperatorId == operatorId)
                .OrderBy(b => b.BookingDate)
                .ToListAsync();
        }

        // 12. Process a refund for a cancelled booking
        public async Task<RefundResultDTO> ProcessRefundAsync(int bookingId, decimal amount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Find the booking with its original payment info and user details
                var booking = await _context.Bookings
                    .Include(b => b.Payment)
                    .Include(b => b.User)
                    .Include(b => b.Route)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                // 2. Strict Validation: Must be approved by admin
                if (booking == null || booking.Status != "CancellationApproved")
                {
                    return new RefundResultDTO
                    {
                        Success = false,
                        Message = "Booking not found or not approved for refund."
                    };
                }

                if (booking.Payment == null)
                {
                    return new RefundResultDTO
                    {
                        Success = false,
                        Message = "Payment record not found for this booking."
                    };
                }

                if (amount > booking.TotalAmount)
                {
                    return new RefundResultDTO
                    {
                        Success = false,
                        Message = "Refund amount cannot exceed the original booking amount."
                    };
                }

                // 3. Update status to Refunded
                booking.Status = "Refunded";

                // 4. Update the existing Payment record to reflect refund
                booking.Payment.PaymentStatus = "Refunded";
                booking.Payment.AmountPaid = -amount;
                booking.Payment.PaymentDate = DateTime.Now;
                booking.Payment.TransactionId = $"REF-{bookingId}-{DateTime.Now.Ticks.ToString().Substring(12)}";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new RefundResultDTO
                {
                    Success = true,
                    Message = "Refund processed successfully.",
                    BookingId = booking.BookingId,
                    PassengerName = booking.User?.FullName ?? "Passenger",
                    PassengerEmail = booking.User?.Email ?? "",
                    RouteName = $"{booking.Route?.SourceCity} to {booking.Route?.DestCity}",
                    RefundAmount = amount,
                    RazorpayPaymentId = booking.Payment.RazorpayPaymentId
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return new RefundResultDTO
                {
                    Success = false,
                    Message = "An error occurred while processing the refund."
                };
            }
        }

        // 12b. Store Razorpay refund ID after successful refund
        public async Task UpdatePaymentRefundIdAsync(int bookingId, string razorpayRefundId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            if (payment != null)
            {
                payment.RazorpayRefundId = razorpayRefundId;
                await _context.SaveChangesAsync();
            }
        }

        // 13. Dashboard stats for Operator overview
        public async Task<OperatorDashboardDTO> GetOperatorDashboardAsync(int operatorId)
        {
            var buses = _context.Buses.Where(b => b.OperatorId == operatorId);

            return new OperatorDashboardDTO
            {
                TotalBuses = await buses.CountAsync(),
                TotalActiveRoutes = await _context.Routes.CountAsync(r => buses.Any(b => b.BusId == r.BusId)),
                BookingsToday = await _context.Bookings.CountAsync(b =>
                    b.JourneyDate.Date == DateTime.Today &&
                    buses.Any(bus => bus.BusId == b.Route.BusId)),
                RevenueThisMonth = await _context.Payments
                    .Where(p => p.PaymentDate.Month == DateTime.Now.Month &&
                                buses.Any(bus => bus.BusId == bus.BusId))
                    .SumAsync(p => p.AmountPaid)
            };
        }

        public async Task<ScheduleCreationResultDTO> CreateBusSchedulesAsync(int routeId, int busId, List<DateTime> dates)
        {
            // 1. Get the duration (MaxDayOffset) for the route being scheduled
            var routeStops = await _context.RouteStops
                .Where(rs => rs.RouteId == routeId)
                .ToListAsync();
            
            int maxOffset = routeStops.Any() ? routeStops.Max(rs => rs.DayOffset) : 0;

            // 2. Perform availability and overlap checks
            var checkResult = await CheckBusAvailabilityAsync(busId, routeId, maxOffset, dates);
            if (!checkResult.Success)
            {
                return checkResult;
            }

            // 3. If all checks pass, create the schedules
            var newSchedules = dates.Select(date => new BusSchedule
            {
                RouteId = routeId,
                BusId = busId,
                ScheduledDate = date.Date,
                IsActive = true
            }).ToList();

            await _context.BusSchedules.AddRangeAsync(newSchedules);
            await _context.SaveChangesAsync();

            // Load with navigation properties for the response
            var scheduleIds = newSchedules.Select(s => s.ScheduleId).ToList();
            var savedSchedules = await _context.BusSchedules
                .Include(bs => bs.Route)
                .Include(bs => bs.Bus)
                .Where(bs => scheduleIds.Contains(bs.ScheduleId))
                .ToListAsync();

            return new ScheduleCreationResultDTO
            {
                Success = true,
                Message = "Schedules created successfully.",
                Schedules = savedSchedules.Select(s => new ScheduleResponseDTO
                {
                    ScheduleId = s.ScheduleId,
                    RouteName = $"{s.Route.SourceCity} to {s.Route.DestCity}",
                    BusName = s.Bus.BusName,
                    ScheduledDate = s.ScheduledDate,
                    IsActive = s.IsActive
                }).ToList()
            };
        }

        public async Task<List<BusSchedule>> GetSchedulesByOperatorAsync(int operatorId)
        {
            return await _context.BusSchedules
                .Include(bs => bs.Route)
                .Include(bs => bs.Bus)
                .Where(bs => bs.Bus.OperatorId == operatorId && bs.IsActive)
                .OrderBy(bs => bs.ScheduledDate)
                .ToListAsync();
        }

        public async Task<List<BusSchedule>> GetSchedulesByBusIdAsync(int busId, int operatorId)
        {
            return await _context.BusSchedules
                .Include(bs => bs.Route)
                .Include(bs => bs.Bus)
                .Where(bs => bs.BusId == busId && bs.Bus.OperatorId == operatorId)
                .OrderBy(bs => bs.ScheduledDate)
                .ToListAsync();
        }

        public async Task<bool> DeleteScheduleAsync(int scheduleId)
        {
            var schedule = await _context.BusSchedules.FindAsync(scheduleId);
            if (schedule == null) return false;

            schedule.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ScheduleDeactivationResultDTO> DeactivateScheduleWithCheckAsync(int scheduleId)
        {
            var schedule = await _context.BusSchedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            if (schedule == null)
            {
                return new ScheduleDeactivationResultDTO
                {
                    Success = false,
                    Message = $"Schedule with ID {scheduleId} not found."
                };
            }

            // Check for active bookings on this schedule's route and date
            var activeBookingsCount = await _context.Bookings
                .Include(b => b.BookedSeats)
                    .ThenInclude(bs => bs.Seat)
                .Where(b => b.RouteId == schedule.RouteId &&
                           b.JourneyDate.Date == schedule.ScheduledDate.Date &&
                           b.Status == "Confirmed" &&
                           b.BookedSeats.Any(bs => bs.Seat.BusId == schedule.BusId))
                .CountAsync();

            if (activeBookingsCount > 0)
            {
                return new ScheduleDeactivationResultDTO
                {
                    Success = false,
                    Message = $"Cannot deactivate this schedule. There are {activeBookingsCount} active booking(s) for this bus on this day. Please reassign a different bus to this route before deactivating.",
                    HasActiveBookings = true,
                    ActiveBookingsCount = activeBookingsCount,
                    ScheduleId = schedule.ScheduleId,
                    BusId = schedule.BusId,
                    BusName = schedule.Bus.BusName,
                    RouteId = schedule.RouteId,
                    RouteName = $"{schedule.Route.SourceCity} to {schedule.Route.DestCity}",
                    ScheduledDate = schedule.ScheduledDate
                };
            }

            // No active bookings, proceed with deactivation
            schedule.IsActive = false;
            await _context.SaveChangesAsync();

            return new ScheduleDeactivationResultDTO
            {
                Success = true,
                Message = "Schedule deactivated successfully.",
                HasActiveBookings = false,
                ActiveBookingsCount = 0,
                ScheduleId = schedule.ScheduleId,
                BusId = schedule.BusId,
                BusName = schedule.Bus.BusName,
                RouteId = schedule.RouteId,
                RouteName = $"{schedule.Route.SourceCity} to {schedule.Route.DestCity}",
                ScheduledDate = schedule.ScheduledDate
            };
        }

        public async Task<ReassignBusResultDTO> ReassignBusToScheduleAsync(int scheduleId, int newBusId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var schedule = await _context.BusSchedules
                    .Include(s => s.Bus)
                    .Include(s => s.Route)
                        .ThenInclude(r => r.RouteStops)
                    .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    return new ReassignBusResultDTO
                    {
                        Success = false,
                        Message = $"Schedule with ID {scheduleId} not found."
                    };
                }

                var newBus = await _context.Buses
                    .Include(b => b.SeatConfigs)
                    .FirstOrDefaultAsync(b => b.BusId == newBusId && b.IsActive);

                if (newBus == null)
                {
                    return new ReassignBusResultDTO
                    {
                        Success = false,
                        Message = $"New bus with ID {newBusId} not found or is not active."
                    };
                }

                // Verify both buses belong to the same operator
                if (schedule.Bus.OperatorId != newBus.OperatorId)
                {
                    return new ReassignBusResultDTO
                    {
                        Success = false,
                        Message = "Cannot reassign to a bus owned by a different operator."
                    };
                }

                // 1. Get the duration (MaxDayOffset) for the journey being reassigned
                int maxOffset = schedule.Route.RouteStops.Any() ? schedule.Route.RouteStops.Max(rs => rs.DayOffset) : 0;
                var newStart = schedule.ScheduledDate.Date;
                var newEnd = newStart.AddDays(maxOffset);

                // 2. Check for overlaps with other existing schedules for the new bus
                var availability = await CheckBusAvailabilityAsync(newBusId, schedule.RouteId, maxOffset, new List<DateTime> { schedule.ScheduledDate.Date }, scheduleId);
                if (!availability.Success)
                {
                    return new ReassignBusResultDTO
                    {
                        Success = false,
                        Message = availability.Message
                    };
                }

                // Get all active bookings for the old bus on this schedule
                var bookingsToTransfer = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.BookedSeats)
                        .ThenInclude(bs => bs.Seat)
                    .Where(b => b.RouteId == schedule.RouteId &&
                               b.JourneyDate.Date == schedule.ScheduledDate.Date &&
                               b.Status == "Confirmed" &&
                               b.BookedSeats.Any(bs => bs.Seat.BusId == schedule.BusId))
                    .ToListAsync();

                // Get seat mapping from old bus to new bus by seat number
                var oldBusSeats = await _context.SeatConfigs
                    .Where(s => s.BusId == schedule.BusId)
                    .ToDictionaryAsync(s => s.SeatNumber, s => s.SeatId);

                var newBusSeats = await _context.SeatConfigs
                    .Where(s => s.BusId == newBusId)
                    .ToDictionaryAsync(s => s.SeatNumber, s => s.SeatId);

                // Check if new bus has all the required seat numbers
                foreach (var booking in bookingsToTransfer)
                {
                    foreach (var bookedSeat in booking.BookedSeats)
                    {
                        var seatNumber = bookedSeat.Seat.SeatNumber;
                        if (!newBusSeats.ContainsKey(seatNumber))
                        {
                            return new ReassignBusResultDTO
                            {
                                Success = false,
                                Message = $"New bus does not have seat {seatNumber}. Please ensure the new bus has compatible seat configuration."
                            };
                        }
                    }
                }

                // Transfer bookings: Update booked seats to point to new bus seats
                foreach (var booking in bookingsToTransfer)
                {
                    foreach (var bookedSeat in booking.BookedSeats)
                    {
                        var seatNumber = bookedSeat.Seat.SeatNumber;
                        bookedSeat.SeatId = newBusSeats[seatNumber];
                    }
                }

                var oldBusId = schedule.BusId;
                var oldBusName = schedule.Bus.BusName;
                var oldBusNumber = schedule.Bus.BusNumber;

                // Update the schedule to use the new bus
                schedule.BusId = newBusId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ReassignBusResultDTO
                {
                    Success = true,
                    Message = $"Bus reassigned successfully. {bookingsToTransfer.Count} booking(s) transferred to the new bus.",
                    ScheduleId = schedule.ScheduleId,
                    OldBusId = oldBusId,
                    OldBusName = oldBusName,
                    OldBusNumber = oldBusNumber,
                    NewBusId = newBusId,
                    NewBusName = newBus.BusName,
                    NewBusNumber = newBus.BusNumber,
                    RouteName = $"{schedule.Route.SourceCity} to {schedule.Route.DestCity}",
                    ScheduledDate = schedule.ScheduledDate,
                    BookingsTransferred = bookingsToTransfer.Count,
                    AffectedBookings = bookingsToTransfer.Select(b => new AffectedBookingDTO
                    {
                        BookingId = b.BookingId,
                        PassengerName = b.User?.FullName ?? "Passenger",
                        PassengerEmail = b.User?.Email ?? ""
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ReassignBusResultDTO
                {
                    Success = false,
                    Message = $"Failed to reassign bus: {ex.Message}"
                };
            }
        }

        // Feedback Management
        public async Task<List<OperatorFeedbackDTO>> GetOperatorFeedbacksAsync(int operatorId)
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Bus)
                .Include(f => f.Booking)
                    .ThenInclude(b => b.Route)
                .Where(f => f.Bus.OperatorId == operatorId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new OperatorFeedbackDTO
                {
                    FeedbackId = f.FeedbackId,
                    BookingId = f.BookingId,
                    BusId = f.BusId,
                    BusName = f.Bus.BusName,
                    BusNumber = f.Bus.BusNumber,
                    PassengerName = f.User.FullName,
                    PassengerEmail = f.User.Email,
                    RouteName = $"{f.Booking.Route.SourceCity} to {f.Booking.Route.DestCity}",
                    JourneyDate = f.Booking.JourneyDate,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt,
                    OperatorResponse = f.OperatorResponse,
                    RespondedAt = f.RespondedAt
                })
                .ToListAsync();
        }

        public async Task<OperatorFeedbackSummaryDTO> GetOperatorFeedbackSummaryAsync(int operatorId)
        {
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Bus)
                .Where(f => f.Bus.OperatorId == operatorId)
                .ToListAsync();

            var ratingGroups = feedbacks.GroupBy(f => f.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            return new OperatorFeedbackSummaryDTO
            {
                TotalFeedbacks = feedbacks.Count,
                PendingResponses = feedbacks.Count(f => string.IsNullOrEmpty(f.OperatorResponse)),
                AverageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 0,
                FiveStarCount = ratingGroups.GetValueOrDefault(5, 0),
                FourStarCount = ratingGroups.GetValueOrDefault(4, 0),
                ThreeStarCount = ratingGroups.GetValueOrDefault(3, 0),
                TwoStarCount = ratingGroups.GetValueOrDefault(2, 0),
                OneStarCount = ratingGroups.GetValueOrDefault(1, 0)
            };
        }

        public async Task<(bool success, string message)> RespondToFeedbackAsync(int operatorId, int feedbackId, string response)
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.Bus)
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId && f.Bus.OperatorId == operatorId);

            if (feedback == null) 
                return (false, "Feedback not found or you don't have permission to respond.");

            // Check if already responded
            if (!string.IsNullOrEmpty(feedback.OperatorResponse))
                return (false, "This feedback has already been responded to.");

            feedback.OperatorResponse = response;
            feedback.RespondedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, "Response submitted successfully.");
        }

        // Bus Information Methods
        public async Task<BusBookingStatusDTO?> GetBusBookingStatusAsync(int busId, int operatorId)
        {
            var bus = await _context.Buses
                .Include(b => b.SeatConfigs)
                .FirstOrDefaultAsync(b => b.BusId == busId && b.OperatorId == operatorId);

            if (bus == null) return null;

            var schedules = await _context.BusSchedules
                .Include(s => s.Route)
                .Where(s => s.BusId == busId && s.IsActive)
                .OrderBy(s => s.ScheduledDate)
                .ToListAsync();

            var scheduleBookings = new List<ScheduleBookingStatusDTO>();

            foreach (var schedule in schedules)
            {
                var bookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.BoardingStop)
                    .Include(b => b.DroppingStop)
                    .Include(b => b.BookedSeats)
                        .ThenInclude(bs => bs.Seat)
                    .Where(b => b.RouteId == schedule.RouteId &&
                               b.JourneyDate.Date == schedule.ScheduledDate.Date &&
                               b.BookedSeats.Any(bs => bs.Seat.BusId == busId))
                    .ToListAsync();

                var confirmedBookings = bookings.Where(b => b.Status == "Confirmed").ToList();
                var cancelledBookings = bookings.Where(b => b.Status == "Cancelled" || b.Status == "CancellationApproved").ToList();
                var bookedSeatsCount = confirmedBookings.SelectMany(b => b.BookedSeats).Count();

                var passengerDetails = confirmedBookings.SelectMany(b => 
                    b.BookedSeats.Select(s => new PassengerBookingDetailDTO
                    {
                        BookingId = b.BookingId,
                        PassengerName = s.PassengerName ?? b.User?.FullName ?? "Unknown",
                        PassengerEmail = b.User?.Email ?? "N/A", // Account email
                        PhoneNumber = s.PassengerPhone ?? b.User?.PhoneNumber,
                        Gender = s.Gender ?? b.User?.Gender,
                        SeatNumber = s.Seat?.SeatNumber ?? "??",
                        BoardingStop = b.BoardingStop?.LocationName ?? "N/A",
                        DroppingStop = b.DroppingStop?.LocationName ?? "N/A",
                        Amount = b.TotalAmount / b.BookedSeats.Count,
                        Status = b.Status,
                        BookingDate = b.BookingDate
                    })).ToList();

                scheduleBookings.Add(new ScheduleBookingStatusDTO
                {
                    ScheduleId = schedule.ScheduleId,
                    RouteId = schedule.RouteId,
                    RouteName = $"{schedule.Route.SourceCity} to {schedule.Route.DestCity}",
                    ScheduledDate = schedule.ScheduledDate,
                    TotalSeats = bus.SeatConfigs?.Count ?? 0,
                    BookedSeats = bookedSeatsCount,
                    AvailableSeats = (bus.SeatConfigs?.Count ?? 0) - bookedSeatsCount,
                    CompletedBookings = confirmedBookings.Count,
                    CancelledBookings = cancelledBookings.Count,
                    TotalRevenue = confirmedBookings.Sum(b => b.TotalAmount),
                    PassengerDetails = passengerDetails
                });
            }

            return new BusBookingStatusDTO
            {
                BusId = bus.BusId,
                BusName = bus.BusName,
                BusNumber = bus.BusNumber,
                TotalCapacity = bus.Capacity,
                ScheduleBookings = scheduleBookings
            };
        }

        public async Task<List<OperatorBusListDTO>> GetAllBusesWithRoutesAsync(int operatorId)
        {
            var buses = await _context.Buses
                .Include(b => b.BusAmenities)
                    .ThenInclude(ba => ba.Amenity)
                .Include(b => b.Routes)
                    .ThenInclude(r => r.RouteStops)
                .Where(b => b.OperatorId == operatorId)
                .ToListAsync();

            var feedbacks = await _context.Feedbacks
                .Where(f => f.Bus.OperatorId == operatorId)
                .ToListAsync();

            return buses.Select(b => {
                var busFeedbacks = feedbacks.Where(f => f.BusId == b.BusId).ToList();
                return new OperatorBusListDTO
                {
                    BusId = b.BusId,
                    BusName = b.BusName,
                    BusNumber = b.BusNumber,
                    BusType = b.BusType,
                    Capacity = b.Capacity,
                    IsActive = b.IsActive,
                    Amenities = b.BusAmenities?.Select(ba => ba.Amenity.AmenityName).ToList() ?? new List<string>(),
                    AverageRating = busFeedbacks.Any() ? Math.Round(busFeedbacks.Average(f => f.Rating), 1) : 0,
                    FeedbackCount = busFeedbacks.Count,
                    Routes = b.Routes?.Select(r => new BusRouteDTO
                    {
                        RouteId = r.RouteId,
                        SourceCity = r.SourceCity,
                        DestCity = r.DestCity,
                        BaseFare = r.BaseFare,
                        TotalStops = r.RouteStops?.Count ?? 0
                    }).ToList() ?? new List<BusRouteDTO>()
                };
            }).ToList();
        }

        public async Task<BusDetailDTO?> GetBusDetailAsync(int busId, int operatorId)
        {
            var bus = await _context.Buses
                .Include(b => b.SeatConfigs)
                .Include(b => b.BusAmenities)
                    .ThenInclude(ba => ba.Amenity)
                .Include(b => b.Routes)
                    .ThenInclude(r => r.RouteStops)
                .FirstOrDefaultAsync(b => b.BusId == busId && b.OperatorId == operatorId);

            if (bus == null) return null;

            // Get upcoming schedules with occupancy
            var upcomingSchedules = await _context.BusSchedules
                .Include(s => s.Route)
                .Where(s => s.BusId == busId && s.IsActive && s.ScheduledDate.Date >= DateTime.Today)
                .OrderBy(s => s.ScheduledDate)
                .Take(10)
                .ToListAsync();

            var scheduleOccupancies = new List<ScheduleOccupancyDTO>();

            foreach (var schedule in upcomingSchedules)
            {
                var occupiedSeats = await _context.BookedSeats
                    .Where(bs => bs.Booking.RouteId == schedule.RouteId &&
                                bs.Booking.JourneyDate.Date == schedule.ScheduledDate.Date &&
                                bs.Booking.Status == "Confirmed" &&
                                bs.Seat.BusId == busId)
                    .CountAsync();

                var totalSeats = bus.SeatConfigs?.Count ?? 0;
                var availableSeats = totalSeats - occupiedSeats;
                var occupancyPercentage = totalSeats > 0 ? Math.Round((double)occupiedSeats / totalSeats * 100, 1) : 0;

                scheduleOccupancies.Add(new ScheduleOccupancyDTO
                {
                    ScheduleId = schedule.ScheduleId,
                    RouteName = $"{schedule.Route.SourceCity} to {schedule.Route.DestCity}",
                    ScheduledDate = schedule.ScheduledDate,
                    OccupiedSeats = occupiedSeats,
                    AvailableSeats = availableSeats,
                    OccupancyPercentage = occupancyPercentage
                });
            }

            return new BusDetailDTO
            {
                BusId = bus.BusId,
                BusName = bus.BusName,
                BusNumber = bus.BusNumber,
                BusType = bus.BusType,
                Capacity = bus.Capacity,
                IsActive = bus.IsActive,
                Amenities = bus.BusAmenities?.Select(ba => ba.Amenity.AmenityName).ToList() ?? new List<string>(),
                Seats = bus.SeatConfigs?.Select(s => new BusSeatDetailDTO
                {
                    SeatId = s.SeatId,
                    SeatNumber = s.SeatNumber,
                    SeatType = s.SeatType,
                    AddonFare = s.AddonFare
                }).ToList() ?? new List<BusSeatDetailDTO>(),
                Routes = bus.Routes?.Select(r => new BusRouteDetailDTO
                {
                    RouteId = r.RouteId,
                    SourceCity = r.SourceCity,
                    DestCity = r.DestCity,
                    BaseFare = r.BaseFare,
                    Stops = r.RouteStops?.OrderBy(rs => rs.StopOrder).Select(rs => new RouteStopDetailDTO
                    {
                        StopId = rs.StopId,
                        CityName = rs.CityName,
                        LocationName = rs.LocationName,
                        StopType = rs.StopType,
                        StopOrder = rs.StopOrder,
                        ArrivalTime = rs.ArrivalTime
                    }).ToList() ?? new List<RouteStopDetailDTO>()
                }).ToList() ?? new List<BusRouteDetailDTO>(),
                OccupancySummary = new BusOccupancySummaryDTO
                {
                    TotalSeats = bus.SeatConfigs?.Count ?? 0,
                    UpcomingSchedules = scheduleOccupancies
                }
            };
        }

        public async Task<RouteDetailsDTO?> GetRouteDetailsAsync(int routeId, int operatorId)
        {
            var route = await _context.Routes
                .Include(r => r.Bus)
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.RouteId == routeId && r.Bus.OperatorId == operatorId);

            if (route == null) return null;

            return new RouteDetailsDTO
            {
                RouteId = route.RouteId,
                BusName = route.Bus?.BusName ?? "N/A",
                BusNumber = route.Bus?.BusNumber ?? "N/A",
                SourceCity = route.SourceCity,
                DestCity = route.DestCity,
                BaseFare = route.BaseFare,
                Stops = route.RouteStops.Select(s => new RouteStopDetailsDTO
                {
                    StopId = s.StopId,
                    CityName = s.CityName,
                    LocationName = s.LocationName,
                    StopType = s.StopType,
                    StopOrder = s.StopOrder,
                    ArrivalTime = s.ArrivalTime
                }).OrderBy(s => s.StopOrder).ToList()
            };
        }

        public async Task<OperatorRouteDetailDTO?> GetEnhancedRouteDetailsAsync(int routeId, int operatorId)
        {
            var route = await _context.Routes
                .Include(r => r.Bus)
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.RouteId == routeId && r.Bus.OperatorId == operatorId);

            if (route == null) return null;

            // Count active bookings for this specific route
            // Active = Confirmed status and Journey date is today or later
            var activeBookings = await _context.Bookings
                .Where(b => b.RouteId == routeId && b.Status == "Confirmed" && b.JourneyDate.Date >= DateTime.Today)
                .CountAsync();

            return new OperatorRouteDetailDTO
            {
                RouteId = route.RouteId,
                BusName = route.Bus?.BusName ?? "N/A",
                BusNumber = route.Bus?.BusNumber ?? "N/A",
                SourceCity = route.SourceCity,
                DestCity = route.DestCity,
                BaseFare = route.BaseFare,
                ActiveBookingsCount = activeBookings,
                Stops = route.RouteStops.Select(s => new RouteStopDetailsDTO
                {
                    StopId = s.StopId,
                    CityName = s.CityName,
                    LocationName = s.LocationName,
                    StopType = s.StopType,
                    StopOrder = s.StopOrder,
                    ArrivalTime = s.ArrivalTime
                }).OrderBy(s => s.StopOrder).ToList()
            };
        }

        private async Task<ScheduleCreationResultDTO> CheckBusAvailabilityAsync(int busId, int routeId, int maxOffset, List<DateTime> dates, int? excludeScheduleId = null)
        {
            // 1. Sort dates to check for overlaps within the batch itself (only applies if multiple dates provided)
            var sortedDates = dates.Select(d => d.Date).OrderBy(d => d).ToList();
            if (sortedDates.Count > 1)
            {
                for (int i = 0; i < sortedDates.Count - 1; i++)
                {
                    var currentDeparture = sortedDates[i];
                    var currentArrival = currentDeparture.AddDays(maxOffset);
                    var nextDeparture = sortedDates[i + 1];

                    if (nextDeparture <= currentArrival)
                    {
                        return new ScheduleCreationResultDTO
                        {
                            Success = false,
                            Message = $"Wait Time Rule: Journey starting {currentDeparture:dd MMM} takes {maxOffset + 1} day(s) and reaches destination on {currentArrival:dd MMM}. The next run on {nextDeparture:dd MMM} overlaps with the previous journey."
                        };
                    }
                }
            }

            // 2. Check for overlaps with existing database schedules
            var minDate = sortedDates.First().AddDays(-10); // buffer
            var maxDate = sortedDates.Last().AddDays(10);  // buffer
            var existingSchedules = await _context.BusSchedules
                .Include(bs => bs.Route)
                    .ThenInclude(r => r.RouteStops)
                .Where(bs => bs.BusId == busId && bs.ScheduleId != (excludeScheduleId ?? -1) &&
                             bs.ScheduledDate.Date >= minDate && bs.ScheduledDate.Date <= maxDate)
                .ToListAsync();

            foreach (var date in dates)
            {
                var newStart = date.Date;
                var newEnd = newStart.AddDays(maxOffset);

                foreach (var existing in existingSchedules)
                {
                    var existingStart = existing.ScheduledDate.Date;
                    
                    // Exact match check (Unique Constraint Prevention)
                    if (existing.RouteId == routeId && existingStart == newStart)
                    {
                        return new ScheduleCreationResultDTO
                        {
                            Success = false,
                            Message = existing.IsActive 
                                ? $"Already Scheduled: This bus is already allocated to this route on {newStart:dd MMM yyyy}."
                                : $"Conflict: An inactive schedule for this route already exists on {newStart:dd MMM yyyy}."
                        };
                    }

                    // Overlap check (Only for active schedules)
                    if (existing.IsActive)
                    {
                        int existingMaxOffset = existing.Route.RouteStops.Any() ? existing.Route.RouteStops.Max(rs => rs.DayOffset) : 0;
                        var existingEnd = existingStart.AddDays(existingMaxOffset);

                        // Overlap check: Math.Max(Start1, Start2) <= Math.Min(End1, End2)
                        if (Math.Max(newStart.Ticks, existingStart.Ticks) <= Math.Min(newEnd.Ticks, existingEnd.Ticks))
                        {
                            return new ScheduleCreationResultDTO
                            {
                                Success = false,
                                Message = $"Conflict: Bus is still in transit for route '{existing.Route.SourceCity} to {existing.Route.DestCity}'. Trip duration: {existingStart:dd MMM} to {existingEnd:dd MMM}."
                            };
                        }
                    }
                }
            }

            return new ScheduleCreationResultDTO { Success = true };
        }
    }
}