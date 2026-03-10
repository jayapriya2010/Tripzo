using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripzo.Models;
using Tripzo.DTOs.Passenger; // Organized subfolder
using Tripzo.Repositories;
using Tripzo.Services;

namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Passenger")]
    public class PassengerController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly ITicketPdfService _ticketPdfService;
        private readonly IEmailService _emailService;

        public PassengerController(
            IBookingRepository bookingRepo,
            ITicketPdfService ticketPdfService,
            IEmailService emailService)
        {
            _bookingRepo = bookingRepo;
            _ticketPdfService = ticketPdfService;
            _emailService = emailService;
        }

        // 1. Search: Finds routes based on origin, destination, and date
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BusSearchResultDTO>>> SearchBuses([FromQuery] SearchBusDTO search)
        {
            // Validate: Date must be from today onwards
            if (search.TravelDate.Date < DateTime.Today)
                return BadRequest(new { message = "Travel date cannot be in the past." });

            // Validate: FromCity and ToCity cannot be the same
            if (search.FromCity.Equals(search.ToCity, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Source and destination cities cannot be the same." });

            var routes = await _bookingRepo.SearchRoutesAsync(search.FromCity, search.ToCity, search.TravelDate);

            // Return 404 if no routes found
            if (routes == null || routes.Count == 0)
                return NotFound(new { message = $"No buses found from {search.FromCity} to {search.ToCity} on {search.TravelDate:yyyy-MM-dd}." });

            var results = new List<BusSearchResultDTO>();

            foreach (var r in routes)
            {
                // Calculate available seats for this route on the travel date
                var availableSeats = await _bookingRepo.GetAvailableSeatsCountAsync(r.BusId, r.RouteId, search.TravelDate);

                // Get actual amenities from the database
                var amenities = r.Bus?.BusAmenities?
                    .Where(ba => ba.Amenity != null)
                    .Select(ba => ba.Amenity.AmenityName)
                    .ToList() ?? new List<string>();

                results.Add(new BusSearchResultDTO
                {
                    RouteId = r.RouteId,
                    BusName = r.Bus?.BusName,
                    BusType = r.Bus?.BusType,
                    DepartureTime = r.RouteStops.FirstOrDefault(s => s.StopOrder == 1)?.ArrivalTime ?? TimeSpan.Zero,
                    Fare = r.BaseFare,
                    Amenities = amenities,
                    AvailableSeats = availableSeats
                });
            }

            return Ok(results);
        }

        // 2. Seat Map: Provides the visual layout with availability
        [HttpGet("seats")]
        public async Task<ActionResult<List<SeatLayoutDTO>>> GetSeatMap(int busId, int routeId, DateTime travelDate)
        {
            // Validate: IDs must be positive
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            if (routeId <= 0)
                return BadRequest(new { message = "Route ID must be a positive number." });

            // Validate: Date must be from today onwards
            if (travelDate.Date < DateTime.Today)
                return BadRequest(new { message = "Travel date cannot be in the past." });

            // Consolidates physical layout and occupied status for 'Cross Mark' UI
            var layout = await _bookingRepo.GetSeatLayoutAsync(busId, routeId, travelDate);

            // Return 404 if no seats found (invalid bus)
            if (layout == null || layout.Count == 0)
                return NotFound(new { message = $"No seat configuration found for Bus ID {busId}." });

            return Ok(layout);
        }

        // 3. Reservation: Processes multi-seat booking and payment
        [HttpPost("book")]
        public async Task<ActionResult<BookingResponseDTO>> CreateReservation([FromBody] BookingRequestDTO request)
        {
            // Validate: Journey date must be from today onwards
            if (request.JourneyDate.Date < DateTime.Today)
                return BadRequest(new { message = "Journey date cannot be in the past." });

            // Validate: Boarding and dropping stops cannot be the same
            if (request.BoardingStopId == request.DroppingStopId)
                return BadRequest(new { message = "Boarding and dropping stops cannot be the same." });

            try
            {
                var booking = new Booking
                {
                    RouteId = request.RouteId,
                    UserId = request.UserId,
                    JourneyDate = request.JourneyDate,
                    BoardingStopId = request.BoardingStopId,
                    DroppingStopId = request.DroppingStopId,
                    TotalAmount = request.TotalPaid,
                    Status = "Confirmed",
                    BookingDate = DateTime.Now
                };

                // Uses a Transaction to ensure all seats are booked or none
                var result = await _bookingRepo.CreateBookingAsync(booking, request.SelectedSeatIds);

                if (result == null)
                {
                    return BadRequest(new { message = "Booking failed. Please check if seats are available and try again." });
                }

                // Generate and send ticket email
                var ticketDetails = await _bookingRepo.GetBookingDetailsForTicketAsync(result.BookingId);
                if (ticketDetails != null)
                {
                    var pdfBytes = _ticketPdfService.GenerateTicketPdf(ticketDetails);
                    await _emailService.SendTicketEmailAsync(
                        ticketDetails.PassengerEmail,
                        ticketDetails.PassengerName,
                        pdfBytes,
                        ticketDetails.BookingId);
                }

                return Ok(new BookingResponseDTO
                {
                    BookingId = result.BookingId,
                    PNR = $"TRPZ{result.BookingId}{DateTime.Now.Ticks.ToString().Substring(10)}",
                    Status = "Confirmed",
                    TotalAmount = result.TotalAmount
                });
            }
            catch (ApplicationException ex)
            {
                // Business logic exceptions with user-friendly messages
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log and return detailed error for debugging
                var errorMessage = ex.InnerException != null
                    ? $"{ex.Message} - Inner: {ex.InnerException.Message}"
                    : ex.Message;
                return BadRequest(new { message = errorMessage });
            }
        }

        // 4. History: Allows users to view past and manage current bookings
        [HttpGet("history/{userId}")]
        public async Task<ActionResult<IEnumerable<PassengerHistoryDTO>>> GetHistory(int userId)
        {
            // Validate: User ID must be positive
            if (userId <= 0)
                return BadRequest(new { message = "User ID must be a positive number." });

            var history = await _bookingRepo.GetPassengerHistoryAsync(userId);

            // Return 404 if no bookings found
            if (history == null || !history.Any())
                return NotFound(new { message = $"No booking history found for User ID {userId}." });

            var historyDtos = history.Select(h => new PassengerHistoryDTO
            {
                BookingId = h.BookingId,
                RouteName = $"{h.Route?.SourceCity} to {h.Route?.DestCity}",
                BusNumber = h.Route?.Bus?.BusNumber,
                JourneyDate = h.JourneyDate,
                Status = h.Status,
                Amount = h.TotalAmount
            });

            return Ok(historyDtos);
        }

        // 5. Cancellation: Request refund for a booking
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelTicket([FromBody] CancelBookingDTO request)
        {
            var result = await _bookingRepo.CancelBookingAsync(request.BookingId, request.UserId);

            if (!result)
            {
                return NotFound(new { message = "Booking not found, already cancelled, or does not belong to you." });
            }

            return Ok(new { message = "Ticket cancelled successfully. Your refund request has been sent to the operator." });
        }
    }
}