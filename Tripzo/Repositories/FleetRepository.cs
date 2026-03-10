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
    }
}