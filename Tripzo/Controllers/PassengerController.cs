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
        private readonly IRazorpayService _razorpayService;

        public PassengerController(
            IBookingRepository bookingRepo,
            ITicketPdfService ticketPdfService,
            IEmailService emailService,
            IRazorpayService razorpayService)
        {
            _bookingRepo = bookingRepo;
            _ticketPdfService = ticketPdfService;
            _emailService = emailService;
            _razorpayService = razorpayService;
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
                    BusId = sr.BusId,
                    BusName = sr.Bus?.BusName,
                    BusType = sr.Bus?.BusType,
                    DepartureTime = sr.Route?.RouteStops?.FirstOrDefault(s => s.StopOrder == 1)?.ArrivalTime ?? TimeSpan.Zero,
                    Fare = sr.Route?.BaseFare ?? 0,
                    DepartureDateTime = sr.DepartureDateTime,
                    ArrivalDateTime = sr.ArrivalDateTime,
                    HasNextDayArrival = sr.HasNextDayArrival,
                    Amenities = amenities,
                    AvailableSeats = availableSeats,
                    AverageRating = averageRating,
                    TotalReviews = totalReviews,
                    BoardingStops = sr.Route?.RouteStops?
                        .Where(s => s.StopType == "Boarding")
                        .Select(s => new RouteStopDTO {
                            StopId = s.StopId,
                            CityName = s.CityName,
                            LocationName = s.LocationName,
                            ArrivalTime = s.ArrivalTime
                        }).ToList(),
                    DroppingStops = sr.Route?.RouteStops?
                        .Where(s => s.StopType == "Dropping")
                        .Select(s => new RouteStopDTO {
                            StopId = s.StopId,
                            CityName = s.CityName,
                            LocationName = s.LocationName,
                            ArrivalTime = s.ArrivalTime
                        }).ToList()
                });
            }

            // Apply optional filters
            var filtered = results.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search.BusType))
            {
                filtered = filtered.Where(r => string.Equals(r.BusType ?? string.Empty, search.BusType, StringComparison.OrdinalIgnoreCase));
            }

            if (search.MinFare.HasValue)
            {
                filtered = filtered.Where(r => r.Fare >= search.MinFare.Value);
            }

            if (search.MaxFare.HasValue)
            {
                filtered = filtered.Where(r => r.Fare <= search.MaxFare.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.Amenities))
            {
                var wanted = search.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(a => a.ToLowerInvariant())
                    .ToList();

                filtered = filtered.Where(r => wanted.All(w => r.Amenities.Any(a => a != null && a.ToLowerInvariant() == w)));
            }

            // Pagination
            var pageNumber = Math.Max(1, search.PageNumber);
            var pageSize = Math.Clamp(search.PageSize, 1, 50);
            var totalItems = filtered.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var paged = filtered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new
            {
                Items = paged,
                Meta = new
                {
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                }
            };

            return Ok(response);
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

        // 3. Step 1 - Create Razorpay Order: Calculates fare and creates a payment order
        [HttpPost("create-order")]
        public async Task<ActionResult<CreateOrderResponseDTO>> CreateOrder([FromBody] CreateOrderRequestDTO request)
        {
            // Validate: Journey date must be from today onwards
            if (request.JourneyDate.Date < DateTime.Today)
                return BadRequest(new { message = "Journey date cannot be in the past." });

            // Validate: Boarding and dropping stops cannot be the same
            if (request.BoardingStopId == request.DroppingStopId)
                return BadRequest(new { message = "Boarding and dropping stops cannot be the same." });

            try
            {
                // Calculate the correct total amount server-side from selected seats
                var calculatedTotal = await _bookingRepo.CalculateTotalFareAsync(request.RouteId, request.Passengers);

                if (calculatedTotal <= 0)
                    return BadRequest(new { message = "Could not calculate fare. Please verify route and seat selections." });

                // Create Razorpay order with the server-calculated amount
                var receiptId = $"rcpt_{request.RouteId}_{DateTime.Now.Ticks}";
                var orderResult = await _razorpayService.CreateOrderAsync(calculatedTotal, receiptId);

                if (!orderResult.Success)
                    return BadRequest(new { message = orderResult.ErrorMessage });

                return Ok(new CreateOrderResponseDTO
                {
                    OrderId = orderResult.OrderId!,
                    Amount = calculatedTotal,
                    Currency = "INR",
                    RazorpayKeyId = _razorpayService.GetKeyId()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to create payment order: {ex.Message}" });
            }
        }

        // 4. Step 2 - Verify Payment & Confirm Booking: Validates Razorpay payment and creates the booking
        [HttpPost("verify-payment")]
        public async Task<ActionResult<BookingResponseDTO>> VerifyPaymentAndBook([FromBody] VerifyPaymentDTO request)
        {
            // Validate: Journey date must be from today onwards
            if (request.JourneyDate.Date < DateTime.Today)
                return BadRequest(new { message = "Journey date cannot be in the past." });

            // Validate: Boarding and dropping stops cannot be the same
            if (request.BoardingStopId == request.DroppingStopId)
                return BadRequest(new { message = "Boarding and dropping stops cannot be the same." });

            try
            {
                // Step 1: Verify the Razorpay payment signature (prevents tampering)
                var isValid = _razorpayService.VerifyPaymentSignature(
                    request.RazorpayOrderId,
                    request.RazorpayPaymentId,
                    request.RazorpaySignature);

                if (!isValid)
                    return BadRequest(new { message = "Payment verification failed. Invalid signature." });

                // Step 2: Recalculate total to ensure consistency
                var calculatedTotal = await _bookingRepo.CalculateTotalFareAsync(request.RouteId, request.Passengers);

                if (calculatedTotal <= 0)
                    return BadRequest(new { message = "Could not calculate fare. Please verify route and seat selections." });

                // Step 3: Create the booking
                var booking = new Booking
                {
                    RouteId = request.RouteId,
                    UserId = request.UserId,
                    JourneyDate = request.JourneyDate,
                    BoardingStopId = request.BoardingStopId,
                    DroppingStopId = request.DroppingStopId,
                    TotalAmount = calculatedTotal, // Server-calculated amount
                    Status = "Confirmed",
                    BookingDate = DateTime.Now
                };

                var result = await _bookingRepo.CreateBookingWithRazorpayAsync(
                    booking,
                    request.BusId,
                    request.Passengers,
                    request.RazorpayOrderId,
                    request.RazorpayPaymentId);

                if (result == null)
                    return BadRequest(new { message = "Booking failed. Please check if seats are available and try again." });

                // Generate and send ticket email (don't fail booking if email fails)
                try
                {
                    var ticketDetails = await _bookingRepo.GetBookingDetailsForTicketAsync(result.BookingId);
                    if (ticketDetails != null)
                    {
                        var pdfBytes = _ticketPdfService.GenerateTicketPdf(ticketDetails);
                        await _emailService.SendTicketEmailAsync(
                            request.PrimaryEmail,
                            ticketDetails.PassengerName,
                            pdfBytes,
                            ticketDetails.BookingId);
                    }
                }
                catch
                {
                    // Email failed but booking is confirmed — don't break the response
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
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
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
                Amount = h.TotalAmount,
                BookedSeats = h.BookedSeats?.Select(s => new Tripzo.DTO.Admin.BookedSeatDetailDTO
                {
                    BookedSeatId = s.BookedSeatId,
                    SeatNumber = s.Seat?.SeatNumber ?? "N/A",
                    PassengerName = s.PassengerName,
                    Status = s.Status,
                    CancellationReason = s.CancellationReason
                }).ToList()
            });

            return Ok(historyDtos);
        }

        // 5. Cancellation: Request refund for a booking
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelTicket([FromBody] CancelBookingDTO request)
        {
            // 3. Initiate cancellation
            var result = await _bookingRepo.CancelBookingAsync(request.BookingId, request.UserId, request.Reason, request.SelectedSeatIds);

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

        // 9. Ticket Details: Get full ticket and traveler info for a booking
        [HttpGet("ticket/{bookingId}")]
        public async Task<ActionResult<TicketDTO>> GetTicket(int bookingId)
        {
            if (bookingId <= 0)
                return BadRequest(new { message = "Booking ID must be a positive number." });

            var ticket = await _bookingRepo.GetBookingDetailsForTicketAsync(bookingId);

            if (ticket == null)
                return NotFound(new { message = $"Ticket with ID {bookingId} not found." });

            return Ok(ticket);
        }
    }
}