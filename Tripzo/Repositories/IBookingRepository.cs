using Tripzo.Models;
using Tripzo.DTOs.Passenger;

namespace Tripzo.Repositories
{
    public interface IBookingRepository
    {
        // Search for available journeys (returns scheduled buses for the specific date)
        Task<List<ScheduledRouteDTO>> SearchScheduledRoutesAsync(string fromCity, string toCity, DateTime travelDate);

        // Legacy method - kept for compatibility
        Task<List<Tripzo.Models.Route>> SearchRoutesAsync(string fromCity, string toCity, DateTime travelDate);

        // Consolidated method to get the visual map of seats (Available vs Occupied)
        Task<List<SeatLayoutDTO>> GetSeatLayoutAsync(int busId, int routeId, DateTime travelDate);

        // Get count of available seats for a bus on a specific date
        Task<int> GetAvailableSeatsCountAsync(int busId, int routeId, DateTime travelDate);

        // Calculate total fare server-side from selected seats
        Task<decimal> CalculateTotalFareAsync(int routeId, List<PassengerDetailDTO> passengers);
 
        // Core Booking Transaction (busId is the scheduled bus for that date)
        Task<Booking> CreateBookingAsync(Booking booking, int busId, List<PassengerDetailDTO> passengers);
 
        // Razorpay Booking Transaction (includes Razorpay order/payment IDs)
        Task<Booking> CreateBookingWithRazorpayAsync(Booking booking, int busId, List<PassengerDetailDTO> passengers, string razorpayOrderId, string razorpayPaymentId);

        // Passenger history
        Task<IEnumerable<Booking>> GetPassengerHistoryAsync(int userId);

        // Cancel a booking (with refund logic placeholder)
        Task<CancellationResultDTO> CancelBookingAsync(int bookingId, int userId, string? reason);

        // Get booking details for generating ticket PDF
        Task<TicketDTO?> GetBookingDetailsForTicketAsync(int bookingId);

        // Feedback
        Task<FeedbackResponseDTO?> SubmitFeedbackAsync(int userId, FeedbackRequestDTO request);
        Task<List<FeedbackResponseDTO>> GetUserFeedbacksAsync(int userId);
        Task<bool> HasUserCompletedJourneyAsync(int userId, int bookingId);
        Task<bool> HasFeedbackAsync(int bookingId);

        // Bus Feedback
        Task<BusFeedbackSummaryDTO?> GetBusFeedbackAsync(int busId);
        Task<(double? averageRating, int totalReviews)> GetBusRatingAsync(int busId);
    }
}