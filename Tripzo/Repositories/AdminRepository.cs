using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.DTO.Admin;
using Tripzo.DTOs.Admin;
using Tripzo.Models;


namespace Tripzo.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _context;

        public AdminRepository(AppDbContext context)
        {
            _context = context;
        }

        // 1. Fetch all registered users (Operators and Passengers only - excludes Admin)
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.Role != "Admin")
                .ToListAsync();
        }

        // Deactivate user account (soft delete)
        public async Task<bool> DeactivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (user.Role != "Operator" && user.Role != "Passenger")
            {
                return false;
            }

            user.IsActive = false;
            return await _context.SaveChangesAsync() > 0;
        }

        // Activate user account
        public async Task<bool> ActivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (user.Role != "Operator" && user.Role != "Passenger")
            {
                return false;
            }

            user.IsActive = true;
            return await _context.SaveChangesAsync() > 0;
        }

        // Get all pending cancellations waiting for admin approval
        public async Task<IEnumerable<Booking>> GetPendingCancellationsAsync()
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Route)
                    .ThenInclude(r => r.Bus)
                .Where(b => b.Status == "Cancelled")
                .OrderBy(b => b.BookingDate)
                .ToListAsync();
        }

        // Admin approves cancellation - allows operator to process refund
        public async Task<bool> ApproveCancellationAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null || booking.Status != "Cancelled")
                return false;

            booking.Status = "CancellationApproved";
            return await _context.SaveChangesAsync() > 0;
        }

        // Admin rejects cancellation - reverts to Confirmed
        public async Task<bool> RejectCancellationAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null || booking.Status != "Cancelled")
                return false;

            booking.Status = "Confirmed";
            return await _context.SaveChangesAsync() > 0;
        }

        // Get all routes in the system
        public async Task<IEnumerable<Tripzo.Models.Route>> GetAllRoutesAsync()
        {
            return await _context.Routes
                .Include(r => r.Bus)
                .Include(r => r.RouteStops)
                .OrderBy(r => r.SourceCity)
                .ToListAsync();
        }

        // Get detailed route information including stops
        public async Task<Tripzo.Models.Route> GetRouteDetailsAsync(int routeId)
        {
            return await _context.Routes
                .Include(r => r.Bus)
                .Include(r => r.RouteStops.OrderBy(s => s.StopOrder))
                .FirstOrDefaultAsync(r => r.RouteId == routeId);
        }

        // 3. View every booking in the system for financial auditing
        public async Task<IEnumerable<Booking>> GetGlobalBookingHistoryAsync()
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Route)
                .OrderByDescending(b => b.JourneyDate)
                .ToListAsync();
        }

        // 4. Add new features like "WiFi" or "Sleeper" to the global list
        public async Task<bool> AddAmenityToMasterAsync(AmenityMaster amenity)
        {
            _context.Amenities.Add(amenity);
            return await _context.SaveChangesAsync() > 0;
        }

        // 5. Retrieve all amenities for management
        public async Task<IEnumerable<AmenityMaster>> GetAmenityListAsync()
        {
            return await _context.Amenities.ToListAsync();
        }

        // 6. View system failures caught by the ErrorLog table
        public async Task<IEnumerable<ErrorLog>> GetSystemErrorLogsAsync()
        {
            return await _context.ErrorLogs
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();
        }

        // 7. Maintenance: Remove old logs to keep the database small
        public async Task<bool> ClearOldLogsAsync(DateTime beforeDate)
        {
            var oldLogs = _context.ErrorLogs.Where(e => e.Timestamp < beforeDate);
            _context.ErrorLogs.RemoveRange(oldLogs);
            return await _context.SaveChangesAsync() > 0;
        }

        // 8. Dashboard stats for Admin overview
        public async Task<AdminDashboardDTO> GetDashboardStatsAsync()
        {
            return new AdminDashboardDTO
            {
                TotalPassengers = await _context.Users.CountAsync(u => u.Role == "Passenger" && u.IsActive),
                ActiveOperators = await _context.Users.CountAsync(u => u.Role == "Operator" && u.IsActive),
                TotalBuses = await _context.Buses.CountAsync(),
                TotalRevenue = await _context.Payments.SumAsync(p => p.AmountPaid),
                TodaysBookings = await _context.Bookings.CountAsync(b => b.JourneyDate.Date == DateTime.Today),
                SystemErrorsLast24Hours = await _context.ErrorLogs.CountAsync(e => e.Timestamp >= DateTime.Now.AddDays(-1))
            };
        }
    }
}