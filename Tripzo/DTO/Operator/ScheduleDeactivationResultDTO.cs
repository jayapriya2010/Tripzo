using System.Collections.Generic;

namespace Tripzo.DTOs.Operator
{
    public class ScheduleDeactivationResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool HasActiveBookings { get; set; }
        public int ActiveBookingsCount { get; set; }
        public int ScheduleId { get; set; }
        public int BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
    }

    public class ReassignBusDTO
    {
        public int ScheduleId { get; set; }
        public int NewBusId { get; set; }
    }

    public class ReassignBusResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ScheduleId { get; set; }
        public int OldBusId { get; set; }
        public string OldBusName { get; set; } = string.Empty;
        public string OldBusNumber { get; set; } = string.Empty;
        public int NewBusId { get; set; }
        public string NewBusName { get; set; } = string.Empty;
        public string NewBusNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public int BookingsTransferred { get; set; }
        public List<AffectedBookingDTO> AffectedBookings { get; set; } = new List<AffectedBookingDTO>();
    }

    public class AffectedBookingDTO
    {
        public int BookingId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
    }
}
