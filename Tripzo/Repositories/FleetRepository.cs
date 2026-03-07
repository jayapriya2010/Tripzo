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
            _context.Buses.Add(bus);
            return await _context.SaveChangesAsync() > 0;
        }

        // 2. Fetch all buses for a specific Operator
        public async Task<IEnumerable<Bus>> GetOperatorFleetAsync(int operatorId)
        {
            return await _context.Buses
                .Include(b => b.SeatConfigs)
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

        // 4. Define the physical seat map for a bus
        public async Task<bool> ConfigureBusSeatsAsync(int busId, List<SeatConfig> seats)
        {
            // Validate bus exists
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null) return false;

            // Count existing seats for this bus
            var existingCount = await _context.SeatConfigs.CountAsync(s => s.BusId == busId);
            var newSeatsCount = seats?.Count ?? 0;

            // Enforce capacity: existing + new must not exceed bus capacity
            if (existingCount + newSeatsCount > bus.Capacity)
            {
                return false;
            }

            // Ensure all seats are linked to the correct bus
            foreach (var seat in seats)
            {
                seat.BusId = busId;
            }

            _context.SeatConfigs.AddRange(seats);
            return await _context.SaveChangesAsync() > 0;
        }

        // 5. Create a Route and its Stops in one transaction
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

        // 6. View all routes currently assigned to a bus
        public async Task<IEnumerable<Tripzo.Models.Route>> GetBusRoutesAsync(int busId)
        {
            return await _context.Routes
                .Include(r => r.RouteStops)
                .Where(r => r.BusId == busId)
                .ToListAsync();
        }

        // 6.5. Get approved cancellations for operator's buses
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

        // 7. Process a refund for a cancelled booking (Admin Function)
        
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
                // Log the error using the Admin's error logging system if possible
                await transaction.RollbackAsync();
                return false;
            }
        }

        // 8. Dashboard stats for Operator overview
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
                                buses.Any(bus => bus.BusId == bus.BusId)) // Simplified logic
                    .SumAsync(p => p.AmountPaid)
            };
        }

        public async Task<List<BusSchedule>> CreateBusSchedulesAsync(int routeId, int busId, List<DateTime> dates)
        {
            var schedules = new List<BusSchedule>();
            
            foreach (var date in dates)
            {
                // Check if schedule already exists
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