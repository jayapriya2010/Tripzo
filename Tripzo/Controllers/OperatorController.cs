using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripzo.Models;
using Tripzo.DTOs.Operator;
using Tripzo.DTO.Admin;
using Tripzo.Repositories;
using Tripzo.Services;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Operator")]
    public class OperatorController : ControllerBase
    {
        private readonly IFleetRepository _fleetRepo;
        private readonly IBookingRepository _bookingRepo;
        private readonly ITicketPdfService _ticketPdfService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IRazorpayService _razorpayService;
        private readonly IServiceScopeFactory _scopeFactory;

        public OperatorController(
            IFleetRepository fleetRepo, 
            IBookingRepository bookingRepo,
            ITicketPdfService ticketPdfService,
            IMapper mapper, 
            IEmailService emailService, 
            IRazorpayService razorpayService,
            IServiceScopeFactory scopeFactory)
        {
            _fleetRepo = fleetRepo;
            _bookingRepo = bookingRepo;
            _ticketPdfService = ticketPdfService;
            _mapper = mapper;
            _emailService = emailService;
            _razorpayService = razorpayService;
            _scopeFactory = scopeFactory;
        }

        // 1. Dashboard: Provides performance metrics for the landing page
        [HttpGet("dashboard/{operatorId}")]
        public async Task<ActionResult<OperatorDashboardDTO>> GetDashboard(int operatorId)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var stats = await _fleetRepo.GetOperatorDashboardAsync(operatorId);
            if (stats == null)
                return NotFound(new { message = $"Dashboard data not found for Operator ID {operatorId}." });

            return Ok(stats);
        }

        // 2. Fleet Management: Add a new bus to the system
        [HttpPost("buses")]
        public async Task<IActionResult> AddBus([FromBody] BusCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BusName))
                return BadRequest(new { message = "Bus name is required." });

            if (string.IsNullOrWhiteSpace(dto.BusNumber))
                return BadRequest(new { message = "Bus number is required." });

            var bus = _mapper.Map<Bus>(dto);
            bus.IsActive = true; // Default to active

            var result = await _fleetRepo.AddBusAsync(bus);
            if (result == null) return BadRequest(new { message = "Could not add bus. Bus number may already exist." });
 
            return Ok(new { message = "Bus registered successfully.", busId = result });
        }

        // 3. Seat Configuration: Define the layout (Sleeper/Seater) and AddonFares
        [HttpPost("buses/{busId}/seats")]
        public async Task<IActionResult> ConfigureSeats(int busId, [FromBody] List<SeatConfigDTO> seatDtos)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            if (seatDtos == null || seatDtos.Count == 0)
                return BadRequest(new { message = "At least one seat configuration is required." });

            var seats = seatDtos.Select(s => _mapper.Map<SeatConfig>(s)).ToList();

            var result = await _fleetRepo.ConfigureBusSeatsAsync(busId, seats);

            if (!result.Success)
            {
                if (result.ConflictingSeatNumbers != null && result.ConflictingSeatNumbers.Any())
                {
                    return BadRequest(new
                    {
                        message = result.ErrorMessage,
                        conflictingSeatNumbers = result.ConflictingSeatNumbers
                    });
                }
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Seat layout configured successfully." });
        }

        // 4. Amenity Management: Get all available amenities
        [HttpGet("amenities")]
        public async Task<ActionResult<IEnumerable<AmenityDTO>>> GetAllAmenities()
        {
            var amenities = await _fleetRepo.GetAllAmenitiesAsync();

            if (amenities == null || !amenities.Any())
                return NotFound(new { message = "No amenities found in the system." });

            var amenityDtos = amenities.Select(a => new AmenityDTO
            {
                AmenityId = a.AmenityId,
                AmenityName = a.AmenityName
            });

            return Ok(amenityDtos);
        }

        // 5. Amenity Management: Get amenities for a specific bus
        [HttpGet("buses/{busId}/amenities")]
        public async Task<ActionResult<IEnumerable<AmenityDTO>>> GetBusAmenities(int busId)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            var amenities = await _fleetRepo.GetBusAmenitiesAsync(busId);

            if (amenities == null || !amenities.Any())
                return NotFound(new { message = $"No amenities found for Bus ID {busId}." });

            var amenityDtos = amenities.Select(a => new AmenityDTO
            {
                AmenityId = a.AmenityId,
                AmenityName = a.AmenityName
            });

            return Ok(amenityDtos);
        }

        // 6. Amenity Management: Add amenities to a bus
        [HttpPost("buses/{busId}/amenities")]
        public async Task<IActionResult> AddAmenitiesToBus(int busId, [FromBody] List<int> amenityIds)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            if (amenityIds == null || amenityIds.Count == 0)
                return BadRequest(new { message = "At least one amenity ID is required." });

            var result = await _fleetRepo.AddAmenitiesToBusAsync(busId, amenityIds);
            if (!result) return NotFound(new { message = $"Bus with ID {busId} not found or amenities could not be added." });

            return Ok(new { message = "Amenities added to bus successfully." });
        }

        // 7. Amenity Management: Remove amenities from a bus
        [HttpDelete("buses/{busId}/amenities")]
        public async Task<IActionResult> RemoveAmenitiesFromBus(int busId, [FromBody] List<int> amenityIds)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            if (amenityIds == null || amenityIds.Count == 0)
                return BadRequest(new { message = "At least one amenity ID is required." });

            var result = await _fleetRepo.RemoveAmenitiesFromBusAsync(busId, amenityIds);
            if (!result) return NotFound(new { message = $"Bus with ID {busId} not found or amenities could not be removed." });

            return Ok(new { message = "Amenities removed from bus successfully." });
        }

        // 8. Route Configuration: Create Route and multiple stops together
        [HttpPost("routes")]
        public async Task<IActionResult> CreateRoute([FromBody] RouteCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SourceCity))
                return BadRequest(new { message = "Source city is required." });

            if (string.IsNullOrWhiteSpace(dto.DestCity))
                return BadRequest(new { message = "Destination city is required." });

            if (dto.SourceCity.Equals(dto.DestCity, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Source and destination cities cannot be the same." });

            if (dto.Stops == null || dto.Stops.Count < 2)
                return BadRequest(new { message = "At least two stops (source and destination) are required." });

            var route = _mapper.Map<Tripzo.Models.Route>(dto);
            var stops = dto.Stops.Select(s => _mapper.Map<RouteStop>(s)).ToList();

            var result = await _fleetRepo.DefineRouteWithStopsAsync(route, stops, dto.ScheduleDate);
            if (!result) return BadRequest(new { message = "Error creating route. The bus might be busy on the selected date or does not exist." });

            return Ok(new { message = "Route and stops created successfully." });
        }

        // 9. Refund Management: Process refunds for cancelled tickets via Razorpay
        [HttpPost("refund")]
        public async Task<IActionResult> ProcessRefund([FromBody] RefundRequestDTO dto)
        {
            if (dto.BookingId <= 0)
                return BadRequest(new { message = "Booking ID must be a positive number." });

            if (dto.RefundAmount <= 0)
                return BadRequest(new { message = "Refund amount must be greater than zero." });

            // 1. Mark as cancelled in DB first
            var result = await _fleetRepo.ProcessRefundAsync(dto.BookingId, dto.RefundAmount, dto.SelectedSeatIds);

            if (!result.Success)
                return NotFound(new { message = result.Message });

            // Then, trigger the actual Razorpay refund if we have a payment ID
            if (!string.IsNullOrEmpty(result.RazorpayPaymentId))
            {
                var razorpayRefund = await _razorpayService.ProcessRefundAsync(
                    result.RazorpayPaymentId, dto.RefundAmount);

                if (razorpayRefund.Success)
                {
                    // Store the Razorpay refund ID
                    await _fleetRepo.UpdatePaymentRefundIdAsync(dto.BookingId, razorpayRefund.RefundId!);
                }
                else
                {
                    // Log the Razorpay error but don't fail — our DB refund is still valid
                    // In test mode, Razorpay refunds may behave differently
                }
            }
            // Send refund initiated email to passenger
            if (!string.IsNullOrEmpty(result.PassengerEmail))
            {
                try
                {
                    // Generate updated PDF ticket ONLY for partial cancellations (remaining seats exist)
                    byte[]? pdfAttachment = null;
                    if (!string.IsNullOrEmpty(result.SeatNumbers))
                    {
                        var ticketDetails = await _bookingRepo.GetBookingDetailsForTicketAsync(result.BookingId);
                        if (ticketDetails != null)
                        {
                            pdfAttachment = _ticketPdfService.GenerateTicketPdf(ticketDetails);
                        }
                    }

                    await _emailService.SendRefundInitiatedEmailAsync(
                        result.PassengerEmail,
                        result.PassengerName,
                        result.BookingId,
                        result.RouteName,
                        result.RefundAmount,
                        result.SeatNumbers,
                        pdfAttachment);
                }
                catch
                {
                    // Log email error but don't fail the refund
                }
            }

            return Ok(new { message = result.Message });
        }

        // 10. View Approved Cancellations for Refund Processing
        [HttpGet("approved-cancellations/{operatorId}")]
        public async Task<ActionResult<IEnumerable<ApprovedCancellationDTO>>> GetApprovedCancellations(int operatorId)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var cancellations = await _fleetRepo.GetApprovedCancellationsForOperatorAsync(operatorId);

            if (cancellations == null || !cancellations.Any())
                return NotFound(new { message = $"No approved cancellations found for Operator ID {operatorId}." });

            var dtos = cancellations.Select(b => new ApprovedCancellationDTO
            {
                BookingId = b.BookingId,
                PassengerName = b.User?.FullName ?? "Unknown",
                PassengerEmail = b.User?.Email ?? "Unknown",
                RouteName = $"{b.Route?.SourceCity} to {b.Route?.DestCity}",
                JourneyDate = b.JourneyDate,
                RefundAmount = b.BookedSeats?.Where(s => s.Status == "CancellationApproved").Count() * (b.TotalAmount / (b.BookedSeats?.Count() ?? 1)) ?? 0,
                CancellationDate = b.BookingDate,
                CancellationReason = b.CancellationReason,
                BookedSeats = b.BookedSeats?.Where(s => s.Status == "CancellationApproved").Select(s => new Tripzo.DTO.Admin.BookedSeatDetailDTO
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

        // 11. Fleet View: List all buses belonging to the operator with paging
        [HttpGet("fleet/{operatorId}")]
        public async Task<ActionResult<PagedResultDTO<BusDTO>>> GetFleet(int operatorId, [FromQuery] PaginationFilterDTO filter)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var pagedResult = await _fleetRepo.GetOperatorFleetAsync(operatorId, filter);

            if (pagedResult.Items == null || !pagedResult.Items.Any())
                return NotFound(new { message = $"No buses found for Operator ID {operatorId} matching your search." });

            var fleetDtos = _mapper.Map<IEnumerable<BusDTO>>(pagedResult.Items);
            
            return Ok(new PagedResultDTO<BusDTO>
            {
                Items = fleetDtos.ToList(),
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            });
        }

        // 12. Status Management: Toggle Bus (Soft Delete)
        [HttpPatch("buses/{busId}/status")]
        public async Task<IActionResult> ToggleBusStatus(int busId, [FromQuery] bool isActive)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            var result = await _fleetRepo.UpdateBusStatusAsync(busId, isActive);
            if (!result) return NotFound(new { message = $"Bus with ID {busId} not found." });

            return Ok(new { message = $"Bus visibility updated to {(isActive ? "Active" : "Inactive")}." });
        }

        [HttpPost("schedule")]
        public async Task<ActionResult<ScheduleCreationResultDTO>> ScheduleBus([FromBody] ScheduleBusDTO dto)
        {
            if (dto.RouteId <= 0)
                return BadRequest(new { message = "Route ID must be a positive number." });
 
            if (dto.BusId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });
 
            if (dto.ScheduledDates == null || dto.ScheduledDates.Count == 0)
                return BadRequest(new { message = "At least one scheduled date is required." });
 
            // Validate all dates are in the future
            var pastDates = dto.ScheduledDates.Where(d => d.Date < DateTime.Today).ToList();
            if (pastDates.Any())
                return BadRequest(new { message = "Scheduled dates cannot be in the past." });
 
            var result = await _fleetRepo.CreateBusSchedulesAsync(dto.RouteId, dto.BusId, dto.ScheduledDates);
 
            if (!result.Success)
            {
                // Return a specific status for inactive conflicts so the UI can prompt for reactivation
                if (result.IsInactiveConflict)
                {
                    return Conflict(result);
                }
                return BadRequest(new { message = result.Message });
            }
 
            return Ok(result);
        }

        [HttpPost("schedule/reactivate/{scheduleId}")]
        public async Task<ActionResult<ScheduleCreationResultDTO>> ReactivateSchedule(int scheduleId)
        {
            if (scheduleId <= 0)
                return BadRequest(new { message = "Schedule ID must be a positive number." });

            var result = await _fleetRepo.ReactivateScheduleAsync(scheduleId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpGet("schedules")]
        public async Task<ActionResult<PagedResultDTO<ScheduleResponseDTO>>> GetSchedules([FromQuery] int operatorId, [FromQuery] PaginationFilterDTO filter)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var pagedResult = await _fleetRepo.GetSchedulesByOperatorAsync(operatorId, filter);

            if (pagedResult.Items == null || !pagedResult.Items.Any())
                return NotFound(new { message = $"No schedules found for Operator ID {operatorId} matching your criteria." });

            var responseItems = pagedResult.Items.Select(s => new ScheduleResponseDTO
            {
                ScheduleId = s.ScheduleId,
                RouteName = $"{s.Route.SourceCity} to {s.Route.DestCity}",
                BusName = s.Bus.BusName,
                ScheduledDate = s.ScheduledDate,
                IsActive = s.IsActive
            }).ToList();

            return Ok(new PagedResultDTO<ScheduleResponseDTO>
            {
                Items = responseItems,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            });
        }

        [HttpGet("schedules/{busId}")]
        public async Task<ActionResult<List<ScheduleResponseDTO>>> GetSchedulesByBusId(int busId, [FromQuery] int operatorId)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var schedules = await _fleetRepo.GetSchedulesByBusIdAsync(busId, operatorId);

            if (schedules == null || schedules.Count == 0)
                return NotFound(new { message = $"No schedules found for Bus ID {busId}." });

            var response = schedules.Select(s => new ScheduleResponseDTO
            {
                ScheduleId = s.ScheduleId,
                RouteName = $"{s.Route.SourceCity} to {s.Route.DestCity}",
                BusName = s.Bus.BusName,
                ScheduledDate = s.ScheduledDate,
                IsActive = s.IsActive
            }).ToList();

            return Ok(response);
        }

        [HttpDelete("schedule/{scheduleId}")]
        public async Task<ActionResult<ScheduleDeactivationResultDTO>> DeleteSchedule(int scheduleId)
        {
            if (scheduleId <= 0)
                return BadRequest(new { message = "Schedule ID must be a positive number." });

            var result = await _fleetRepo.DeactivateScheduleWithCheckAsync(scheduleId);

            if (!result.Success)
            {
                if (result.HasActiveBookings)
                {
                    return Conflict(result);
                }
                return NotFound(new { message = result.Message });
            }

            return Ok(result);
        }

        [HttpPut("schedule/reassign")]
        public async Task<ActionResult<ReassignBusResultDTO>> ReassignBusToSchedule([FromBody] ReassignBusDTO dto)
        {
            if (dto.ScheduleId <= 0)
                return BadRequest(new { message = "Schedule ID must be a positive number." });

            if (dto.NewBusId <= 0)
                return BadRequest(new { message = "New Bus ID must be a positive number." });

            var result = await _fleetRepo.ReassignBusToScheduleAsync(dto.ScheduleId, dto.NewBusId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            // Fire-and-forget email notifications to passengers with updated tickets
            if (result.AffectedBookings != null && result.AffectedBookings.Any())
            {
                _ = Task.Run(async () =>
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var scopedBookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                        var scopedPdfService = scope.ServiceProvider.GetRequiredService<ITicketPdfService>();
                        var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        foreach (var booking in result.AffectedBookings)
                        {
                            try
                            {
                                var ticketDetails = await scopedBookingRepo.GetBookingDetailsForTicketAsync(booking.BookingId);
                                if (ticketDetails != null)
                                {
                                    var pdfBytes = scopedPdfService.GenerateTicketPdf(ticketDetails);
                                    await scopedEmailService.SendBusReassignmentEmailAsync(
                                        booking.PassengerEmail,
                                        booking.PassengerName,
                                        booking.BookingId,
                                        result.RouteName,
                                        result.ScheduledDate,
                                        $"{result.OldBusName} ({result.OldBusNumber})",
                                        $"{result.NewBusName} ({result.NewBusNumber})",
                                        pdfBytes,
                                        string.Join(", ", ticketDetails.SeatNumbers));
                                }
                            }
                            catch (Exception)
                            {
                                // Catching email errors locally to ensure one failure doesn't stop others
                            }
                        }
                    }
                });
            }

            return Ok(result);
        }

        // Feedback Management

        // 14. Get all feedbacks for operator's buses
        [HttpGet("feedbacks/{operatorId}")]
        public async Task<ActionResult<List<OperatorFeedbackDTO>>> GetFeedbacks(int operatorId)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var feedbacks = await _fleetRepo.GetOperatorFeedbacksAsync(operatorId);

            if (feedbacks == null || feedbacks.Count == 0)
                return NotFound(new { message = $"No feedbacks found for Operator ID {operatorId}." });

            return Ok(feedbacks);
        }

        // 15. Get feedback summary for operator
        [HttpGet("feedbacks/{operatorId}/summary")]
        public async Task<ActionResult<OperatorFeedbackSummaryDTO>> GetFeedbackSummary(int operatorId)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var summary = await _fleetRepo.GetOperatorFeedbackSummaryAsync(operatorId);
            return Ok(summary);
        }

        // 16. Respond to a feedback
        [HttpPost("feedbacks/respond")]
        public async Task<ActionResult> RespondToFeedback([FromBody] FeedbackResponseRequestDTO request)
        {
            if (request.FeedbackId <= 0)
                return BadRequest(new { message = "Feedback ID must be a positive number." });

            if (string.IsNullOrWhiteSpace(request.Response))
                return BadRequest(new { message = "Response cannot be empty." });

            if (request.Response.Length > 1000)
                return BadRequest(new { message = "Response cannot exceed 1000 characters." });

            // Get operator ID from JWT claims
            var operatorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(operatorIdClaim) || !int.TryParse(operatorIdClaim, out int operatorId))
                return Unauthorized(new { message = "Invalid operator token." });

            var (success, message) = await _fleetRepo.RespondToFeedbackAsync(operatorId, request.FeedbackId, request.Response);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        // Bus Information Endpoints

        // 17. Get booking status for a specific bus with schedule paging
        [HttpGet("buses/{busId}/bookings")]
        public async Task<ActionResult<BusBookingStatusDTO>> GetBusBookingStatus(int busId, [FromQuery] int operatorId, [FromQuery] PaginationFilterDTO filter)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var result = await _fleetRepo.GetBusBookingStatusAsync(busId, operatorId, filter);

            if (result == null)
                return NotFound(new { message = $"Bus with ID {busId} not found or does not belong to this operator." });

            return Ok(result);
        }

        // 18. Get all buses with routes for an operator with paging
        [HttpGet("allBuses/{operatorId}")]
        public async Task<ActionResult<PagedResultDTO<OperatorBusListDTO>>> GetAllBusesWithRoutes(int operatorId, [FromQuery] PaginationFilterDTO filter)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var result = await _fleetRepo.GetAllBusesWithRoutesAsync(operatorId, filter);

            if (result.Items == null || !result.Items.Any())
                return NotFound(new { message = $"No buses found for Operator ID {operatorId} matching your criteria." });

            return Ok(result);
        }

        [HttpGet("allRoutes/{operatorId}")]
        public async Task<ActionResult<PagedResultDTO<OperatorRouteDetailDTO>>> GetOperatorRoutes(int operatorId, [FromQuery] PaginationFilterDTO filter)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var result = await _fleetRepo.GetOperatorRoutesAsync(operatorId, filter);

            if (result.Items == null || !result.Items.Any())
                return NotFound(new { message = $"No routes found for Operator ID {operatorId} matching your search." });

            return Ok(result);
        }

        // 19. Get detailed information for a specific bus
        [HttpGet("bus/{busId}")]
        public async Task<ActionResult<BusDetailDTO>> GetBusDetail(int busId, [FromQuery] int operatorId)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var result = await _fleetRepo.GetBusDetailAsync(busId, operatorId);

            if (result == null)
                return NotFound(new { message = $"Bus with ID {busId} not found or does not belong to this operator." });

            return Ok(result);
        }

        // 20. Get route details for operator
        [HttpGet("route-detail/{routeId}")]
        public async Task<ActionResult<OperatorRouteDetailDTO>> GetRouteDetails(int routeId)
        {
            if (routeId <= 0)
                return BadRequest(new { message = "Route ID must be a positive number." });

            var operatorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(operatorIdClaim) || !int.TryParse(operatorIdClaim, out int operatorId))
                return Unauthorized(new { message = "Invalid operator token." });

            var result = await _fleetRepo.GetEnhancedRouteDetailsAsync(routeId, operatorId);

            if (result == null)
                return NotFound(new { message = $"Route with ID {routeId} not found or does not belong to you." });

            return Ok(result);
        }

        // 21. Diagnostic Ping
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { message = "Operator Controller is Reachable" });
    }
}