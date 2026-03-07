using Tripzo.Models;
using Tripzo.DTOs.Passenger;

namespace Tripzo.Repositories
{
    public interface IBookingRepository
    {
        // Search for available journeys
        Task<List<Tripzo.Models.Route>> SearchRoutesAsync(string fromCity, string toCity, DateTime travelDate);

        // Consolidated method to get the visual map of seats (Available vs Occupied)
        Task<List<SeatLayoutDTO>> GetSeatLayoutAsync(int busId, int routeId, DateTime travelDate);

        // Get count of available seats for a bus on a specific date
        Task<int> GetAvailableSeatsCountAsync(int busId, int routeId, DateTime travelDate);

        // Core Booking Transaction
        Task<Booking> CreateBookingAsync(Booking booking, List<int> seatIds);

        // Passenger history
        Task<IEnumerable<Booking>> GetPassengerHistoryAsync(int userId);

        // Cancel a booking (with refund logic placeholder)
        Task<bool> CancelBookingAsync(int bookingId, int userId);
    }
}