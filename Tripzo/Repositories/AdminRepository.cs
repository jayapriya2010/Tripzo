using Microsoft.Data.SqlClient;
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

        // 1.1 Fetch users with pagination and filters
        public async Task<PagedResultDTO<User>> GetAllUsersAsync(UserFilterDTO filter)
        {
            var query = _context.Users.Where(u => u.Role != "Admin").AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                query = query.Where(u => u.Role == filter.Role);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Gender))
            {
                query = query.Where(u => u.Gender == filter.Gender);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchLower = filter.SearchTerm.ToLower();
                query = query.Where(u => 
                    u.FullName.ToLower().Contains(searchLower) || 
                    u.Email.ToLower().Contains(searchLower));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "email" => filter.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "role" => filter.SortDescending ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
                "isactive" => filter.SortDescending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
                "userid" => filter.SortDescending ? query.OrderByDescending(u => u.UserId) : query.OrderBy(u => u.UserId),
                _ => filter.SortDescending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName)
            };

            // Apply pagination
            var users = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultDTO<User>
            {
                Items = users,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        // 1.2 Get user by ID using Stored Procedure
        public async Task<UserDetailsDTO?> GetUserByIdAsync(int userId)
        {
            var userIdParam = new SqlParameter("@UserId", userId);

            var result = await _context.Database
                .SqlQueryRaw<UserDetailsDTO>("EXEC [dbo].[GetUserById] @UserId", userIdParam)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        // Check if user exists and get their role
        public async Task<(bool exists, string? role)> CheckUserExistsAsync(int userId)
        {
            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.UserId, u.Role })
                .FirstOrDefaultAsync();

            return user == null ? (false, null) : (true, user.Role);
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
        public async Task<CancellationApprovalResultDTO> ApproveCancellationAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Route)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.Status != "Cancelled")
            {
                return new CancellationApprovalResultDTO
                {
                    Success = false,
                    Message = "Booking not found or is not in a cancellable state."
                };
            }

            booking.Status = "CancellationApproved";
            await _context.SaveChangesAsync();

            return new CancellationApprovalResultDTO
            {
                Success = true,
                Message = "Cancellation approved. Operator can now process the refund.",
                BookingId = booking.BookingId,
                PassengerName = booking.User?.FullName ?? "Passenger",
                PassengerEmail = booking.User?.Email ?? "",
                RouteName = $"{booking.Route?.SourceCity} to {booking.Route?.DestCity}",
                Amount = booking.TotalAmount
            };
        }

        // Admin rejects cancellation - reverts to Confirmed
        public async Task<CancellationRejectionResultDTO> RejectCancellationAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Route)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.Status != "Cancelled")
            {
                return new CancellationRejectionResultDTO
                {
                    Success = false,
                    Message = "Booking not found or cannot be rejected."
                };
            }

            booking.Status = "Confirmed";
            await _context.SaveChangesAsync();

            return new CancellationRejectionResultDTO
            {
                Success = true,
                Message = "Cancellation rejected. Booking has been restored to Confirmed status.",
                BookingId = booking.BookingId,
                PassengerName = booking.User?.FullName ?? "Passenger",
                PassengerEmail = booking.User?.Email ?? "",
                RouteName = $"{booking.Route?.SourceCity} to {booking.Route?.DestCity}",
                JourneyDate = booking.JourneyDate,
                Amount = booking.TotalAmount
            };
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

        // Get all routes with pagination and filters
        public async Task<PagedResultDTO<Tripzo.Models.Route>> GetAllRoutesAsync(RouteFilterDTO filter)
        {
            var query = _context.Routes
                .Include(r => r.Bus)
                .Include(r => r.RouteStops)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SourceCity))
            {
                var sourceLower = filter.SourceCity.ToLower();
                query = query.Where(r => r.SourceCity.ToLower().Contains(sourceLower));
            }

            if (!string.IsNullOrWhiteSpace(filter.DestCity))
            {
                var destLower = filter.DestCity.ToLower();
                query = query.Where(r => r.DestCity.ToLower().Contains(destLower));
            }

            if (!string.IsNullOrWhiteSpace(filter.BusName))
            {
                var busNameLower = filter.BusName.ToLower();
                query = query.Where(r => r.Bus != null && r.Bus.BusName.ToLower().Contains(busNameLower));
            }

            if (!string.IsNullOrWhiteSpace(filter.BusNumber))
            {
                var busNumberLower = filter.BusNumber.ToLower();
                query = query.Where(r => r.Bus != null && r.Bus.BusNumber.ToLower().Contains(busNumberLower));
            }

            if (filter.MinFare.HasValue)
            {
                query = query.Where(r => r.BaseFare >= filter.MinFare.Value);
            }

            if (filter.MaxFare.HasValue)
            {
                query = query.Where(r => r.BaseFare <= filter.MaxFare.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchLower = filter.SearchTerm.ToLower();
                query = query.Where(r =>
                    r.SourceCity.ToLower().Contains(searchLower) ||
                    r.DestCity.ToLower().Contains(searchLower) ||
                    (r.Bus != null && r.Bus.BusName.ToLower().Contains(searchLower)) ||
                    (r.Bus != null && r.Bus.BusNumber.ToLower().Contains(searchLower)));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "destcity" => filter.SortDescending ? query.OrderByDescending(r => r.DestCity) : query.OrderBy(r => r.DestCity),
                "busname" => filter.SortDescending ? query.OrderByDescending(r => r.Bus.BusName) : query.OrderBy(r => r.Bus.BusName),
                "busnumber" => filter.SortDescending ? query.OrderByDescending(r => r.Bus.BusNumber) : query.OrderBy(r => r.Bus.BusNumber),
                "basefare" => filter.SortDescending ? query.OrderByDescending(r => r.BaseFare) : query.OrderBy(r => r.BaseFare),
                "routeid" => filter.SortDescending ? query.OrderByDescending(r => r.RouteId) : query.OrderBy(r => r.RouteId),
                _ => filter.SortDescending ? query.OrderByDescending(r => r.SourceCity) : query.OrderBy(r => r.SourceCity)
            };

            // Apply pagination
            var routes = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultDTO<Tripzo.Models.Route>
            {
                Items = routes,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        // Get detailed route information including stops
        public async Task<Tripzo.Models.Route> GetRouteDetailsAsync(int routeId)
        {
            return await _context.Routes
                .Include(r => r.Bus)
                .Include(r => r.RouteStops.OrderBy(s => s.StopOrder))
                .FirstOrDefaultAsync(r => r.RouteId == routeId);
        }

        // Get route by ID using Stored Procedure
        public async Task<RouteDetailsDTO?> GetRouteByIdSpAsync(int routeId)
        {
            var routeIdParam = new SqlParameter("@RouteId", routeId);

            // Execute stored procedure and get route details
            var routeResult = await _context.Database
                .SqlQueryRaw<RouteSpDTO>("EXEC [dbo].[GetRouteById] @RouteId", routeIdParam)
                .ToListAsync();

            var route = routeResult.FirstOrDefault();
            if (route == null)
                return null;

            // Execute stored procedure again to get stops (separate query for multiple result sets)
            var stopsResult = await _context.Database
                .SqlQueryRaw<RouteStopSpDTO>(
                    "SELECT StopId, RouteId, CityName, LocationName, StopType, StopOrder, ArrivalTime FROM RouteStops WHERE RouteId = @RouteId ORDER BY StopOrder",
                    new SqlParameter("@RouteId", routeId))
                .ToListAsync();

            return new RouteDetailsDTO
            {
                RouteId = route.RouteId,
                BusName = route.BusName ?? "N/A",
                BusNumber = route.BusNumber ?? "N/A",
                SourceCity = route.SourceCity,
                DestCity = route.DestCity,
                BaseFare = route.BaseFare,
                Stops = stopsResult.Select(s => new RouteStopDetailsDTO
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