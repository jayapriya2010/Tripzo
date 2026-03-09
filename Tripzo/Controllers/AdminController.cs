using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripzo.DTO.Admin;
using Tripzo.DTOs.Admin;
using Tripzo.Models;
using Tripzo.Repositories;



namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _adminRepo;

        public AdminController(IAdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
        }

        // Example: Retrieve authenticated user info from JWT claims
        // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // var email = User.FindFirst(ClaimTypes.Email)?.Value;
        // var role = User.FindFirst(ClaimTypes.Role)?.Value;

        // 1. Get All Users with Pagination and Filters (For User Management)
        [HttpGet("users")]
        public async Task<ActionResult<PagedResultDTO<UserListDTO>>> GetUsers([FromQuery] UserFilterDTO filter)
        {
            var pagedResult = await _adminRepo.GetAllUsersAsync(filter);
            
            var userDtos = pagedResult.Items.Select(u => new UserListDTO
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                Gender = u.Gender,
                IsActive = u.IsActive
            }).ToList();

            return Ok(new PagedResultDTO<UserListDTO>
            {
                Items = userDtos,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            });
        }

        // 1.1 Get User Details by ID (Using Stored Procedure)
        [HttpGet("users/{userId}")]
        public async Task<ActionResult<UserDetailsDTO>> GetUserById(int userId)
        {
            var user = await _adminRepo.GetUserByIdAsync(userId);
            
            if (user == null)
                return NotFound($"User with ID {userId} not found or is an Admin user.");

            return Ok(user);
        }

        // 2. Deactivate User Account (Operator or Passenger only)
        [HttpPut("deactivate-user/{userId}")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var result = await _adminRepo.DeactivateUserAsync(userId);
            if (!result) return NotFound("User not found or cannot be deactivated.");

            return Ok(new { message = "User account deactivated successfully." });
        }

        // 3. Activate User Account (Operator or Passenger only)
        [HttpPut("activate-user/{userId}")]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            var result = await _adminRepo.ActivateUserAsync(userId);
            if (!result) return NotFound("User not found or cannot be activated.");

            return Ok(new { message = "User account activated successfully." });
        }

        // 4. View Pending Cancellation Requests
        [HttpGet("pending-cancellations")]
        public async Task<ActionResult<IEnumerable<PendingCancellationDTO>>> GetPendingCancellations()
        {
            var cancellations = await _adminRepo.GetPendingCancellationsAsync();
            var dtos = cancellations.Select(b => new PendingCancellationDTO
            {
                BookingId = b.BookingId,
                PassengerName = b.User?.FullName ?? "Unknown",
                PassengerEmail = b.User?.Email ?? "Unknown",
                RouteName = $"{b.Route?.SourceCity} to {b.Route?.DestCity}",
                BusNumber = b.Route?.Bus?.BusNumber ?? "N/A",
                JourneyDate = b.JourneyDate,
                TotalAmount = b.TotalAmount,
                CancellationDate = b.BookingDate,
                Status = b.Status
            });

            return Ok(dtos);
        }

        // 5. Approve Cancellation Request
        [HttpPut("approve-cancellation/{bookingId}")]
        public async Task<IActionResult> ApproveCancellation(int bookingId)
        {
            var result = await _adminRepo.ApproveCancellationAsync(bookingId);
            if (!result) return NotFound("Booking not found or cannot be approved.");

            return Ok(new { message = "Cancellation approved. Operator can now process the refund." });
        }

        // 6. Reject Cancellation Request
        [HttpPut("reject-cancellation/{bookingId}")]
        public async Task<IActionResult> RejectCancellation(int bookingId)
        {
            var result = await _adminRepo.RejectCancellationAsync(bookingId);
            if (!result) return NotFound("Booking not found or cannot be rejected.");

            return Ok(new { message = "Cancellation rejected. Booking reverted to Confirmed status." });
        }

        // 7. View All Routes with Pagination and Filters
        [HttpGet("routes")]
        public async Task<ActionResult<PagedResultDTO<RouteDetailsDTO>>> GetAllRoutes([FromQuery] RouteFilterDTO filter)
        {
            var pagedResult = await _adminRepo.GetAllRoutesAsync(filter);

            var routeDtos = pagedResult.Items.Select(r => new RouteDetailsDTO
            {
                RouteId = r.RouteId,
                BusName = r.Bus?.BusName ?? "N/A",
                BusNumber = r.Bus?.BusNumber ?? "N/A",
                SourceCity = r.SourceCity,
                DestCity = r.DestCity,
                BaseFare = r.BaseFare,
                Stops = r.RouteStops?.Select(s => new RouteStopDetailsDTO
                {
                    StopId = s.StopId,
                    CityName = s.CityName,
                    LocationName = s.LocationName,
                    StopType = s.StopType,
                    StopOrder = s.StopOrder,
                    ArrivalTime = s.ArrivalTime
                }).OrderBy(s => s.StopOrder).ToList() ?? new List<RouteStopDetailsDTO>()
            }).ToList();

            return Ok(new PagedResultDTO<RouteDetailsDTO>
            {
                Items = routeDtos,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            });
        }

        // 8. View Route Details by ID (Using Stored Procedure)
        [HttpGet("routes/{routeId}")]
        public async Task<ActionResult<RouteDetailsDTO>> GetRouteDetails(int routeId)
        {
            var route = await _adminRepo.GetRouteByIdSpAsync(routeId);
            
            if (route == null)
                return NotFound($"Route with ID {routeId} not found.");

            return Ok(route);
        }

        // 9. Global Booking Audit List
        [HttpGet("bookings")]
        public async Task<ActionResult<IEnumerable<GlobalBookingDTO>>> GetGlobalBookings()
        {
            var bookings = await _adminRepo.GetGlobalBookingHistoryAsync();
            var bookingDtos = bookings.Select(b => new GlobalBookingDTO
            {
                BookingId = b.BookingId,
                PassengerName = b.User?.FullName ?? "Unknown",
                RouteName = $"{b.Route?.SourceCity} to {b.Route?.DestCity}",
                JourneyDate = b.JourneyDate,
                TotalAmount = b.TotalAmount,
                Status = b.Status
            });

            return Ok(bookingDtos);
        }

        // 10. Manage Master Amenities (Add new features like 'WiFi')
        [HttpPost("amenities")]
        public async Task<IActionResult> CreateAmenity([FromBody] CreateAmenityDTO dto)
        {
            var amenity = new AmenityMaster { AmenityName = dto.AmenityName };
            var result = await _adminRepo.AddAmenityToMasterAsync(amenity);

            if (!result) return BadRequest("Could not add amenity.");
            return Ok(new { message = "Amenity added to master list." });
        }

        // 11. System Health: View Error Logs
        [HttpGet("logs")]
        public async Task<ActionResult<IEnumerable<ErrorLogDTO>>> GetLogs()
        {
            var logs = await _adminRepo.GetSystemErrorLogsAsync();
            var logDtos = logs.Select(l => new ErrorLogDTO
            {
                LogId = l.LogId,
                Message = l.Message,
                Source = l.Source,
                Timestamp = l.Timestamp
            });

            return Ok(logDtos);
        }

        // 12. Maintenance: Clear old logs
        [HttpDelete("logs/clear")]
        public async Task<IActionResult> ClearLogs([FromQuery] DateTime beforeDate)
        {
            var result = await _adminRepo.ClearOldLogsAsync(beforeDate);
            return Ok(new { message = "Old logs cleared successfully." });
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardDTO>> GetDashboard()
        {
            var stats = await _adminRepo.GetDashboardStatsAsync();
            return Ok(stats);
        }
    }
}