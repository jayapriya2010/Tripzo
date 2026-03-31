using Tripzo.DTOs.Operator;
using Tripzo.Models;
using Tripzo.DTO.Admin;

namespace Tripzo.Repositories
{
    public interface IFleetRepository
    {
        // Fleet Management
        Task<int?> AddBusAsync(Bus bus);
        Task<IEnumerable<Bus>> GetOperatorFleetAsync(int operatorId);
        Task<bool> UpdateBusStatusAsync(int busId, bool status);

        // Amenity Management
        Task<bool> AddAmenitiesToBusAsync(int busId, List<int> amenityIds);
        Task<bool> RemoveAmenitiesFromBusAsync(int busId, List<int> amenityIds);
        Task<IEnumerable<AmenityMaster>> GetAllAmenitiesAsync();
        Task<IEnumerable<AmenityMaster>> GetBusAmenitiesAsync(int busId);

        // Seat Layout Management
        Task<SeatConfigResult> ConfigureBusSeatsAsync(int busId, List<SeatConfig> seats);

        // Route & Schedule Management
        Task<bool> DefineRouteWithStopsAsync(Tripzo.Models.Route route, List<RouteStop> stops);
        Task<IEnumerable<Tripzo.Models.Route>> GetBusRoutesAsync(int busId);

        // Refund Management
        Task<IEnumerable<Booking>> GetApprovedCancellationsForOperatorAsync(int operatorId);
        Task<RefundResultDTO> ProcessRefundAsync(int bookingId, decimal amount);
        Task UpdatePaymentRefundIdAsync(int bookingId, string razorpayRefundId);

        // Schedule Management
        Task<ScheduleCreationResultDTO> CreateBusSchedulesAsync(int routeId, int busId, List<DateTime> dates);
        Task<List<BusSchedule>> GetSchedulesByOperatorAsync(int operatorId);
        Task<List<BusSchedule>> GetSchedulesByBusIdAsync(int busId, int operatorId);
        Task<bool> DeleteScheduleAsync(int scheduleId);
        Task<ScheduleDeactivationResultDTO> DeactivateScheduleWithCheckAsync(int scheduleId);
        Task<ReassignBusResultDTO> ReassignBusToScheduleAsync(int scheduleId, int newBusId);

        // Feedback Management
        Task<List<OperatorFeedbackDTO>> GetOperatorFeedbacksAsync(int operatorId);
        Task<OperatorFeedbackSummaryDTO> GetOperatorFeedbackSummaryAsync(int operatorId);
        Task<(bool success, string message)> RespondToFeedbackAsync(int operatorId, int feedbackId, string response);

        // Bus Information
        Task<BusBookingStatusDTO?> GetBusBookingStatusAsync(int busId, int operatorId);
        Task<List<OperatorBusListDTO>> GetAllBusesWithRoutesAsync(int operatorId);
        Task<BusDetailDTO?> GetBusDetailAsync(int busId, int operatorId);

        Task<OperatorDashboardDTO> GetOperatorDashboardAsync(int operatorId);
        Task<RouteDetailsDTO?> GetRouteDetailsAsync(int routeId, int operatorId);
        Task<OperatorRouteDetailDTO?> GetEnhancedRouteDetailsAsync(int routeId, int operatorId);
    }
}
