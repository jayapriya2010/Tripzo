using Tripzo.DTOs.Operator;
using Tripzo.Models;

namespace Tripzo.Repositories
{
    public interface IFleetRepository
    {
        // Fleet Management
        Task<bool> AddBusAsync(Bus bus);
        Task<IEnumerable<Bus>> GetOperatorFleetAsync(int operatorId);
        Task<bool> UpdateBusStatusAsync(int busId, bool status);

        // Seat Layout Management
        Task<bool> ConfigureBusSeatsAsync(int busId, List<SeatConfig> seats);

        // Route & Schedule Management
        Task<bool> DefineRouteWithStopsAsync(Tripzo.Models.Route route, List<RouteStop> stops);
        Task<IEnumerable<Tripzo.Models.Route>> GetBusRoutesAsync(int busId);
        
        // Refund Management
        Task<IEnumerable<Booking>> GetApprovedCancellationsForOperatorAsync(int operatorId);
        Task<bool> ProcessRefundAsync(int bookingId, decimal amount);
        
        // Schedule Management
        Task<List<BusSchedule>> CreateBusSchedulesAsync(int routeId, int busId, List<DateTime> dates);
        Task<List<BusSchedule>> GetSchedulesByOperatorAsync(int operatorId);
        Task<bool> DeleteScheduleAsync(int scheduleId);

        Task<OperatorDashboardDTO> GetOperatorDashboardAsync(int operatorId);

    }
}