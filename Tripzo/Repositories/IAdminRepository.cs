using Tripzo.DTO.Admin;
using Tripzo.DTOs.Admin;
using Tripzo.Models;

namespace Tripzo.Repositories
{
    public interface IAdminRepository
    {
        // User & Role Management
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ActivateUserAsync(int userId);

        // Cancellation Approval Workflow
        Task<IEnumerable<Booking>> GetPendingCancellationsAsync();
        Task<bool> ApproveCancellationAsync(int bookingId);
        Task<bool> RejectCancellationAsync(int bookingId);

        // Route Management
        Task<IEnumerable<Tripzo.Models.Route>> GetAllRoutesAsync();
        Task<Tripzo.Models.Route> GetRouteDetailsAsync(int routeId);

        // System-Wide Audit
        Task<IEnumerable<Booking>> GetGlobalBookingHistoryAsync();

        // Master Data Management
        Task<bool> AddAmenityToMasterAsync(AmenityMaster amenity);
        Task<IEnumerable<AmenityMaster>> GetAmenityListAsync();

        // System Health & Debugging
        Task<IEnumerable<ErrorLog>> GetSystemErrorLogsAsync();
        Task<bool> ClearOldLogsAsync(DateTime beforeDate);
        Task<AdminDashboardDTO> GetDashboardStatsAsync(); 
    }
}