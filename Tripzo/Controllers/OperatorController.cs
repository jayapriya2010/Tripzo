using Microsoft.AspNetCore.Mvc;
using Tripzo.Models;
using Tripzo.DTOs.Operator;
using Tripzo.Repositories;
using AutoMapper;

namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Operator")] // Uncomment this for production security
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
            var stats = await _fleetRepo.GetOperatorDashboardAsync(operatorId);
            return Ok(stats);
        }

        // 2. Fleet Management: Add a new bus to the system
        [HttpPost("buses")]
        public async Task<IActionResult> AddBus([FromBody] BusCreateDTO dto)
        {
            var bus = _mapper.Map<Bus>(dto);
            bus.IsActive = true; // Default to active

            var result = await _fleetRepo.AddBusAsync(bus);
            if (!result) return BadRequest("Could not add bus.");

            return Ok(new { message = "Bus registered successfully." });
        }

        // 3. Seat Configuration: Define the layout (Sleeper/Seater) and AddonFares
        [HttpPost("buses/{busId}/seats")]
        public async Task<IActionResult> ConfigureSeats(int busId, [FromBody] List<SeatConfigDTO> seatDtos)
        {
            var seats = seatDtos.Select(s => _mapper.Map<SeatConfig>(s)).ToList();

            var result = await _fleetRepo.ConfigureBusSeatsAsync(busId, seats);
            if (!result) return BadRequest("Failed to configure seats.");

            return Ok(new { message = "Seat layout configured successfully." });
        }

        // 4. Route Configuration: Create Route and multiple stops together
        [HttpPost("routes")]
        public async Task<IActionResult> CreateRoute([FromBody] RouteCreateDTO dto)
        {
            var route = _mapper.Map<Tripzo.Models.Route>(dto);

            var stops = dto.Stops.Select(s => _mapper.Map<RouteStop>(s)).ToList();

            var result = await _fleetRepo.DefineRouteWithStopsAsync(route, stops);
            if (!result) return BadRequest("Error creating route or schedule stops.");

            return Ok(new { message = "Route and stops created successfully." });
        }

        // 5. Refund Management: Process refunds for cancelled tickets
        [HttpPost("refund")]
        public async Task<IActionResult> ProcessRefund([FromBody] RefundRequestDTO dto)
        {
            // Business rule: only operators can trigger the refund logic for their buses
            var result = await _fleetRepo.ProcessRefundAsync(dto.BookingId, dto.RefundAmount);
            if (!result) return BadRequest("Refund processing failed. Check booking status or admin approval.");

            return Ok(new { message = "Refund processed successfully." });
        }

        // 5.5. View Approved Cancellations for Refund Processing
        [HttpGet("approved-cancellations/{operatorId}")]
        public async Task<ActionResult<IEnumerable<ApprovedCancellationDTO>>> GetApprovedCancellations(int operatorId)
        {
            var cancellations = await _fleetRepo.GetApprovedCancellationsForOperatorAsync(operatorId);
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

        // 6. Fleet View: List all buses belonging to the operator
        [HttpGet("fleet/{operatorId}")]
        public async Task<ActionResult<IEnumerable<BusDTO>>> GetFleet(int operatorId)
        {
            var fleet = await _fleetRepo.GetOperatorFleetAsync(operatorId);
            var fleetDtos = _mapper.Map<IEnumerable<BusDTO>>(fleet);

            return Ok(fleetDtos);
        }

        // 7. Status Management: Toggle Bus (Soft Delete)
        [HttpPatch("buses/{busId}/status")]
        public async Task<IActionResult> ToggleBusStatus(int busId, [FromQuery] bool isActive)
        {
            var result = await _fleetRepo.UpdateBusStatusAsync(busId, isActive);
            if (!result) return NotFound("Bus not found.");

            return Ok(new { message = $"Bus visibility updated to {(isActive ? "Active" : "Inactive")}." });
        }

        [HttpPost("schedule")]
        public async Task<ActionResult<List<ScheduleResponseDTO>>> ScheduleBus([FromBody] ScheduleBusDTO dto)
        {
            var schedules = await _fleetRepo.CreateBusSchedulesAsync(dto.RouteId, dto.BusId, dto.ScheduledDates);
            
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
            var schedules = await _fleetRepo.GetSchedulesByOperatorAsync(operatorId);
            
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
            var result = await _fleetRepo.DeleteScheduleAsync(scheduleId);
            if (!result) return NotFound("Schedule not found");
            return Ok("Schedule deleted successfully");
        }
    }
}