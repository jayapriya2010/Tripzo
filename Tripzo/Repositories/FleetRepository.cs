using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.DTOs.Operator;
using Tripzo.Models;

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
        public async Task<bool> AddBusAsync(Bus bus)
        {
            // Check if bus number already exists
            var exists = await _context.Buses.AnyAsync(b => b.BusNumber == bus.BusNumber);
            if (exists)
                return false;

            _context.Buses.Add(bus);
            return await _context.SaveChangesAsync() > 0;
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
        public async Task<bool> DefineRouteWithStopsAsync(Tripzo.Models.Route route, List<RouteStop> stops)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Save the main Route
                _context.Routes.Add(route);
                await _context.SaveChangesAsync(); // Generates RouteId

                // Link and save all stops for this journey
                foreach (var stop in stops)
                {
                    stop.RouteId = route.RouteId;
                    _context.RouteStops.Add(stop);
                }

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
        public async Task<bool> ProcessRefundAsync(int bookingId, decimal amount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Find the booking with its original payment info
                var booking = await _context.Bookings
                    .Include(b => b.Payment)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                // 2. Strict Validation: Must be approved by admin and refund cannot exceed original price
                if (booking == null || booking.Status != "CancellationApproved") return false;
                if (booking.Payment == null) return false;
                if (amount > booking.TotalAmount) return false;

                // 3. Update status to Refunded
                booking.Status = "Refunded";

                // 4. Update the existing Payment record to reflect refund
                booking.Payment.PaymentStatus = "Refunded";
                booking.Payment.AmountPaid = -amount;
                booking.Payment.PaymentDate = DateTime.Now;
                booking.Payment.TransactionId = $"REF-{bookingId}-{DateTime.Now.Ticks.ToString().Substring(12)}";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return false;
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

        public async Task<List<BusSchedule>> CreateBusSchedulesAsync(int routeId, int busId, List<DateTime> dates)
        {
            var schedules = new List<BusSchedule>();
            
            foreach (var date in dates)
            {
                var exists = await _context.BusSchedules
                    .AnyAsync(bs => bs.RouteId == routeId && 
                           bs.BusId == busId && 
                           bs.ScheduledDate.Date == date.Date);
                
                if (!exists)
                {
                    schedules.Add(new BusSchedule
                    {
                        RouteId = routeId,
                        BusId = busId,
                        ScheduledDate = date.Date,
                        IsActive = true
                    });
                }
            }

            if (schedules.Any())
            {
                await _context.BusSchedules.AddRangeAsync(schedules);
                await _context.SaveChangesAsync();

                // Reload with navigation properties
                var scheduleIds = schedules.Select(s => s.ScheduleId).ToList();
                schedules = await _context.BusSchedules
                    .Include(bs => bs.Route)
                    .Include(bs => bs.Bus)
                    .Where(bs => scheduleIds.Contains(bs.ScheduleId))
                    .ToListAsync();
            }

            return schedules;
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

                // Check if new bus is already scheduled for this route on this date
                var conflictingSchedule = await _context.BusSchedules
                    .AnyAsync(s => s.BusId == newBusId &&
                                  s.RouteId == schedule.RouteId &&
                                  s.ScheduledDate.Date == schedule.ScheduledDate.Date &&
                                  s.IsActive &&
                                  s.ScheduleId != scheduleId);

                if (conflictingSchedule)
                {
                    return new ReassignBusResultDTO
                    {
                        Success = false,
                        Message = "The new bus is already scheduled for this route on this date."
                    };
                }

                // Get all active bookings for the old bus on this schedule
                var bookingsToTransfer = await _context.Bookings
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
                    NewBusId = newBusId,
                    NewBusName = newBus.BusName,
                    RouteName = $"{schedule.Route.SourceCity} to {schedule.Route.DestCity}",
                    ScheduledDate = schedule.ScheduledDate,
                    BookingsTransferred = bookingsToTransfer.Count
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
                    .Include(b => b.BookedSeats)
                        .ThenInclude(bs => bs.Seat)
                    .Where(b => b.RouteId == schedule.RouteId &&
                               b.JourneyDate.Date == schedule.ScheduledDate.Date &&
                               b.BookedSeats.Any(bs => bs.Seat.BusId == busId))
                    .ToListAsync();

                var confirmedBookings = bookings.Where(b => b.Status == "Confirmed").ToList();
                var cancelledBookings = bookings.Where(b => b.Status == "Cancelled" || b.Status == "CancellationApproved").ToList();

                var bookedSeatsCount = confirmedBookings.SelectMany(b => b.BookedSeats).Count();

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
                    TotalRevenue = confirmedBookings.Sum(b => b.TotalAmount)
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

            return buses.Select(b => new OperatorBusListDTO
            {
                BusId = b.BusId,
                BusName = b.BusName,
                BusNumber = b.BusNumber,
                BusType = b.BusType,
                Capacity = b.Capacity,
                IsActive = b.IsActive,
                Amenities = b.BusAmenities?.Select(ba => ba.Amenity.AmenityName).ToList() ?? new List<string>(),
                Routes = b.Routes?.Select(r => new BusRouteDTO
                {
                    RouteId = r.RouteId,
                    SourceCity = r.SourceCity,
                    DestCity = r.DestCity,
                    BaseFare = r.BaseFare,
                    TotalStops = r.RouteStops?.Count ?? 0
                }).ToList() ?? new List<BusRouteDTO>()
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
    }
}