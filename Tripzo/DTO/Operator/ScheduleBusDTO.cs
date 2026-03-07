namespace Tripzo.DTOs.Operator
{
    public class ScheduleBusDTO
    {
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public List<DateTime> ScheduledDates { get; set; } = new();
    }

    public class ScheduleResponseDTO
    {
        public int ScheduleId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string BusName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public bool IsActive { get; set; }
    }
}