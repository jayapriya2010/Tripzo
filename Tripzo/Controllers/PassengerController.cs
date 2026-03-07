using Microsoft.AspNetCore.Mvc;
using Tripzo.Models;
using Tripzo.DTOs.Passenger; // Organized subfolder
using Tripzo.Repositories;

namespace Tripzo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Passenger")] // Suggested for secure booking
    public class PassengerController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepo;

        public PassengerController(IBookingRepository bookingRepo)
        {
            _bookingRepo = bookingRepo;
        }

        // 1. Search: Finds routes based on origin, destination, and date
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BusSearchResultDTO>>> SearchBuses([FromQuery] SearchBusDTO search)
        {
            // Requirement: Date must be from today and should be validated
            if (search.TravelDate.Date < DateTime.Today)
                return BadRequest("Travel date cannot be in the past.");

            var routes = await _bookingRepo.SearchRoutesAsync(search.FromCity, search.ToCity, search.TravelDate);

            var results = new List<BusSearchResultDTO>();
            
            foreach (var r in routes)
            {
                // Calculate available seats for this route on the travel date
                var availableSeats = await _bookingRepo.GetAvailableSeatsCountAsync(r.BusId, r.RouteId, search.TravelDate);
                
                results.Add(new BusSearchResultDTO
                {
                    RouteId = r.RouteId,
                    BusName = r.Bus?.BusName,
                    BusType = r.Bus?.BusType,
                    DepartureTime = r.RouteStops.FirstOrDefault(s => s.StopOrder == 1)?.ArrivalTime ?? TimeSpan.Zero,
                    Fare = r.BaseFare,
                    Amenities = new List<string> { "Water Bottle", "Charging Point", "Blanket" },
                    AvailableSeats = availableSeats
                });
            }

            return Ok(results);
        }

        // 2. Seat Map: Provides the visual layout with availability
        [HttpGet("seats")]
        public async Task<ActionResult<List<SeatLayoutDTO>>> GetSeatMap(int busId, int routeId, DateTime travelDate)
        {
            // Consolidates physical layout and occupied status for 'Cross Mark' UI
            var layout = await _bookingRepo.GetSeatLayoutAsync(busId, routeId, travelDate);
            return Ok(layout);
        }

        // 3. Reservation: Processes multi-seat booking and payment
        [HttpPost("book")]
        public async Task<ActionResult<BookingResponseDTO>> CreateReservation([FromBody] BookingRequestDTO request)
        {
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
                    return BadRequest("Booking failed. Please check if seats are available and try again.");
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
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log and return detailed error for debugging
                var errorMessage = ex.InnerException != null 
                    ? $"{ex.Message} - Inner: {ex.InnerException.Message}" 
                    : ex.Message;
                return BadRequest(errorMessage);
            }
        }

        // 4. History: Allows users to view past and manage current bookings
        [HttpGet("history/{userId}")]
        public async Task<ActionResult<IEnumerable<PassengerHistoryDTO>>> GetHistory(int userId)
        {
            var history = await _bookingRepo.GetPassengerHistoryAsync(userId);
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
                return BadRequest("Unable to cancel ticket. It may already be cancelled or not found.");
            }

            return Ok(new { message = "Ticket cancelled successfully. Your refund request has been sent to the operator." });
        }
    }
}