using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripzo.DTO.Admin;
using Tripzo.DTOs.Admin;
using Tripzo.Models;
using Tripzo.Repositories;
using Tripzo.Services;



namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IEmailService _emailService;

        public AdminController(IAdminRepository adminRepo, IEmailService emailService)
        {
            _adminRepo = adminRepo;
            _emailService = emailService;
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
                PhoneNumber = u.PhoneNumber,
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
            if (userId <= 0)
                return BadRequest(new { message = "User ID must be a positive number." });

            // First check if user exists and get their role
            var (exists, role) = await _adminRepo.CheckUserExistsAsync(userId);

            if (!exists)
                return NotFound(new { message = $"User with ID {userId} not found." });

            if (role == "Admin")
                return BadRequest(new { message = "Cannot view Admin user details through this endpoint." });

            var user = await _adminRepo.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = $"User with ID {userId} not found." });

            return Ok(user);
        }

        // 2. Deactivate User Account (Operator or Passenger only)
        [HttpPut("deactivate-user/{userId}")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "User ID must be a positive number." });

            var (exists, role) = await _adminRepo.CheckUserExistsAsync(userId);

            if (!exists)
                return NotFound(new { message = $"User with ID {userId} not found." });

            if (role == "Admin")
                return BadRequest(new { message = "Cannot deactivate Admin users." });

            var result = await _adminRepo.DeactivateUserAsync(userId);
            if (!result) return BadRequest(new { message = "User cannot be deactivated. Only Operator or Passenger accounts can be deactivated." });

            return Ok(new { message = "User account deactivated successfully." });
        }

        // 3. Activate User Account (Operator or Passenger only)
        [HttpPut("activate-user/{userId}")]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "User ID must be a positive number." });

            var (exists, role) = await _adminRepo.CheckUserExistsAsync(userId);

            if (!exists)
                return NotFound(new { message = $"User with ID {userId} not found." });

            if (role == "Admin")
                return BadRequest(new { message = "Cannot activate Admin users through this endpoint." });

            var result = await _adminRepo.ActivateUserAsync(userId);
            if (!result) return BadRequest(new { message = "User cannot be activated. Only Operator or Passenger accounts can be activated." });

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
                Status = b.Status,
                CancellationReason = b.CancellationReason,
                BookedSeats = b.BookedSeats?.Select(s => new BookedSeatDetailDTO
                {
                    BookedSeatId = s.BookedSeatId,
                    SeatNumber = s.Seat?.SeatNumber ?? "N/A",
                    PassengerName = s.PassengerName,
                    Status = s.Status,
                    CancellationReason = s.CancellationReason
                }).ToList()
            });

            return Ok(dtos);
        }

        // 5. Approve Cancellation Request
        [HttpPut("approve-cancellation/{bookingId}")]
        public async Task<IActionResult> ApproveCancellation(int bookingId, [FromBody] List<int>? seatIds = null)
        {
            if (bookingId <= 0)
                return BadRequest(new { message = "Booking ID must be a positive number." });

            var result = await _adminRepo.ApproveCancellationAsync(bookingId, seatIds);

            if (!result.Success)
                return NotFound(new { message = result.Message });

            // Send cancellation approved email to passenger
            if (!string.IsNullOrEmpty(result.PassengerEmail))
            {
                try
                {
                    await _emailService.SendCancellationApprovedEmailAsync(
                        result.PassengerEmail,
                        result.PassengerName,
                        result.BookingId,
                        result.RouteName,
                        result.Amount);
                }
                catch
                {
                    // Log email error but don't fail the approval
                }
            }

            return Ok(new { message = result.Message });
        }

        // 6. Reject Cancellation Request
        [HttpPut("reject-cancellation/{bookingId}")]
        public async Task<IActionResult> RejectCancellation(int bookingId, [FromBody] List<int>? seatIds = null)
        {
            if (bookingId <= 0)
                return BadRequest(new { message = "Booking ID must be a positive number." });

            var result = await _adminRepo.RejectCancellationAsync(bookingId, seatIds);

            if (!result.Success)
                return NotFound(new { message = result.Message });

            // Send cancellation rejected email to passenger
            if (!string.IsNullOrEmpty(result.PassengerEmail))
            {
                try
                {
                    await _emailService.SendCancellationRejectedEmailAsync(
                        result.PassengerEmail,
                        result.PassengerName,
                        result.BookingId,
                        result.RouteName,
                        result.JourneyDate,
                        result.Amount);
                }
                catch
                {
                    // Log email error but don't fail the rejection
                }
            }

            return Ok(new { message = result.Message });
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
            if (routeId <= 0)
                return BadRequest(new { message = "Route ID must be a positive number." });

            var route = await _adminRepo.GetRouteByIdSpAsync(routeId);

            if (route == null)
                return NotFound(new { message = $"Route with ID {routeId} not found." });

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
        [HttpGet("amenities")]
        public async Task<ActionResult<IEnumerable<object>>> GetAmenities()
        {
            var amenities = await _adminRepo.GetAmenityListAsync();
            var dtos = amenities.Select(a => new { a.AmenityId, a.AmenityName });
            return Ok(dtos);
        }

        [HttpPost("amenities")]
        public async Task<IActionResult> CreateAmenity([FromBody] CreateAmenityDTO dto)
        {
            var amenity = new AmenityMaster { AmenityName = dto.AmenityName };
            var result = await _adminRepo.AddAmenityToMasterAsync(amenity);

            if (!result) return BadRequest("Could not add amenity.");
            return Ok(new { message = "Amenity added to master list." });
        }

        // 11. System Health: View Error Logs
        // [HttpGet("logs")]
        // public async Task<ActionResult<IEnumerable<ErrorLogDTO>>> GetLogs()
        // {
        //     var logs = await _adminRepo.GetSystemErrorLogsAsync();
        //     var logDtos = logs.Select(l => new ErrorLogDTO
        //     {
        //         LogId = l.LogId,
        //         Message = l.Message,
        //         Source = l.Source,
        //         Timestamp = l.Timestamp
        //     });

        //     return Ok(logDtos);
        // }

        // 12. Maintenance: Clear old logs
        // [HttpDelete("logs/clear")]
        // public async Task<IActionResult> ClearLogs([FromQuery] DateTime beforeDate)
        // {
        //     var result = await _adminRepo.ClearOldLogsAsync(beforeDate);
        //     return Ok(new { message = "Old logs cleared successfully." });
        // }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardDTO>> GetDashboard()
        {
            var stats = await _adminRepo.GetDashboardStatsAsync();
            return Ok(stats);
        }
    }
}