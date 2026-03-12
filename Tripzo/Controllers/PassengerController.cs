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

            // Use the new schedule-aware search that returns the actual bus for that date
            var scheduledRoutes = await _bookingRepo.SearchScheduledRoutesAsync(search.FromCity, search.ToCity, search.TravelDate);

            // Return 404 if no routes found
            if (scheduledRoutes == null || scheduledRoutes.Count == 0)
                return NotFound(new { message = $"No buses found from {search.FromCity} to {search.ToCity} on {search.TravelDate:yyyy-MM-dd}." });

            var results = new List<BusSearchResultDTO>();

            foreach (var sr in scheduledRoutes)
            {
                // Calculate available seats using the SCHEDULED bus (not route's default bus)
                var availableSeats = await _bookingRepo.GetAvailableSeatsCountAsync(sr.BusId, sr.RouteId, search.TravelDate);

                // Get average rating for the SCHEDULED bus
                var (averageRating, totalReviews) = await _bookingRepo.GetBusRatingAsync(sr.BusId);

                // Get actual amenities from the SCHEDULED bus
                var amenities = sr.Bus?.BusAmenities?
                    .Where(ba => ba.Amenity != null)
                    .Select(ba => ba.Amenity.AmenityName)
                    .ToList() ?? new List<string>();

                results.Add(new BusSearchResultDTO
                {
                    RouteId = sr.RouteId,
                    BusId = sr.BusId, // This is now the SCHEDULED bus, not route's default
                    BusName = sr.Bus?.BusName,
                    BusType = sr.Bus?.BusType,
                    DepartureTime = sr.Route?.RouteStops?.FirstOrDefault(s => s.StopOrder == 1)?.ArrivalTime ?? TimeSpan.Zero,
                    Fare = sr.Route?.BaseFare ?? 0,
                    Amenities = amenities,
                    AvailableSeats = availableSeats,
                    AverageRating = averageRating,
                    TotalReviews = totalReviews
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
                // Pass the BusId from the search result (the scheduled bus for that date)
                var result = await _bookingRepo.CreateBookingAsync(booking, request.BusId, request.SelectedSeatIds);

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

            if (!result.Success)
            {
                return NotFound(new { message = result.Message });
            }

            // Send cancellation request email to passenger
            if (!string.IsNullOrEmpty(result.PassengerEmail))
            {
                try
                {
                    await _emailService.SendCancellationRequestEmailAsync(
                        result.PassengerEmail,
                        result.PassengerName,
                        result.BookingId,
                        result.RouteName,
                        result.JourneyDate,
                        result.Amount);
                }
                catch
                {
                    // Log email error but don't fail the cancellation
                }
            }

            return Ok(new { message = result.Message });
        }

        // 6. Feedback: Submit feedback for a completed journey
        [HttpPost("feedback")]
        public async Task<ActionResult<FeedbackResponseDTO>> SubmitFeedback([FromBody] FeedbackRequestDTO request)
        {
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Invalid user token. Please login again." });

            // Check if journey is completed
            var hasCompleted = await _bookingRepo.HasUserCompletedJourneyAsync(userId, request.BookingId);
            if (!hasCompleted)
                return BadRequest(new { 
                    message = "You can only submit feedback after completing your journey.",
                    hint = "Ensure: 1) Booking belongs to you, 2) Journey date has passed, 3) Booking is not cancelled"
                });

            // Check if feedback already exists
            var hasFeedback = await _bookingRepo.HasFeedbackAsync(request.BookingId);
            if (hasFeedback)
                return BadRequest(new { message = "Feedback has already been submitted for this booking." });

            var feedback = await _bookingRepo.SubmitFeedbackAsync(userId, request);

            if (feedback == null)
                return BadRequest(new { message = "Failed to submit feedback. Please verify booking details." });

            return Ok(feedback);
        }

        // 7. Feedback: Get all feedbacks submitted by the user
        [HttpGet("feedback/{userId}")]
        public async Task<ActionResult<List<FeedbackResponseDTO>>> GetUserFeedbacks(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "User ID must be a positive number." });

            var feedbacks = await _bookingRepo.GetUserFeedbacksAsync(userId);

            if (feedbacks == null || feedbacks.Count == 0)
                return NotFound(new { message = $"No feedbacks found for User ID {userId}." });

            return Ok(feedbacks);
        }

        // 8. Feedback: Get all feedbacks for a specific bus
        [HttpGet("feedback/bus/{busId}")]
        public async Task<ActionResult<BusFeedbackSummaryDTO>> GetBusFeedbacks(int busId)
        {
            if (busId <= 0)
                return BadRequest(new { message = "Bus ID must be a positive number." });

            var feedbackSummary = await _bookingRepo.GetBusFeedbackAsync(busId);

            if (feedbackSummary == null)
                return NotFound(new { message = $"Bus with ID {busId} not found." });

            return Ok(feedbackSummary);
        }
    }
}