using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripzo.Models;
using Tripzo.DTOs.Operator;
using Tripzo.DTO.Admin;
using Tripzo.Repositories;
using AutoMapper;

namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Operator")]
    public class OperatorController : ControllerBase
    {
        private readonly IFleetRepository _fleetRepo;
        private readonly IMapper _mapper;

        public OperatorController(IFleetRepository fleetRepo, IMapper mapper)
        {
            _fleetRepo = fleetRepo;
            _mapper = mapper;
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
            if (!result) return BadRequest(new { message = "Could not add bus. Bus number may already exist." });

            return Ok(new { message = "Bus registered successfully." });
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

            var result = await _fleetRepo.DefineRouteWithStopsAsync(route, stops);
            if (!result) return BadRequest(new { message = "Error creating route. Check if bus exists." });

            return Ok(new { message = "Route and stops created successfully." });
        }

        // 9. Refund Management: Process refunds for cancelled tickets
        [HttpPost("refund")]
        public async Task<IActionResult> ProcessRefund([FromBody] RefundRequestDTO dto)
        {
            if (dto.BookingId <= 0)
                return BadRequest(new { message = "Booking ID must be a positive number." });

            if (dto.RefundAmount <= 0)
                return BadRequest(new { message = "Refund amount must be greater than zero." });

            var result = await _fleetRepo.ProcessRefundAsync(dto.BookingId, dto.RefundAmount);
            if (!result) return NotFound(new { message = "Booking not found, not approved for refund, or already refunded." });

            return Ok(new { message = "Refund processed successfully." });
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
                RefundAmount = b.TotalAmount,
                CancellationDate = b.BookingDate
            });

            return Ok(dtos);
        }

        // 11. Fleet View: List all buses belonging to the operator
        [HttpGet("fleet/{operatorId}")]
        public async Task<ActionResult<IEnumerable<BusDTO>>> GetFleet(int operatorId)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var fleet = await _fleetRepo.GetOperatorFleetAsync(operatorId);

            if (fleet == null || !fleet.Any())
                return NotFound(new { message = $"No buses found for Operator ID {operatorId}." });

            var fleetDtos = _mapper.Map<IEnumerable<BusDTO>>(fleet);
            return Ok(fleetDtos);
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
        public async Task<ActionResult<List<ScheduleResponseDTO>>> ScheduleBus([FromBody] ScheduleBusDTO dto)
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

            var schedules = await _fleetRepo.CreateBusSchedulesAsync(dto.RouteId, dto.BusId, dto.ScheduledDates);

            if (schedules == null || schedules.Count == 0)
                return BadRequest(new { message = "No schedules created. Dates may already be scheduled." });

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

        [HttpGet("schedules")]
        public async Task<ActionResult<List<ScheduleResponseDTO>>> GetSchedules(int operatorId)
        {
            if (operatorId <= 0)
                return BadRequest(new { message = "Operator ID must be a positive number." });

            var schedules = await _fleetRepo.GetSchedulesByOperatorAsync(operatorId);

            if (schedules == null || schedules.Count == 0)
                return NotFound(new { message = $"No schedules found for Operator ID {operatorId}." });

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
        public async Task<ActionResult> DeleteSchedule(int scheduleId)
        {
            if (scheduleId <= 0)
                return BadRequest(new { message = "Schedule ID must be a positive number." });

            var result = await _fleetRepo.DeleteScheduleAsync(scheduleId);
            if (!result) return NotFound(new { message = $"Schedule with ID {scheduleId} not found." });

            return Ok(new { message = "Schedule deleted successfully." });
        }
    }
}