namespace Tripzo.DTOs.Passenger
{
    public class ScheduledRouteDTO
    {
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public Models.Bus Bus { get; set; } = null!;
        public Models.Route Route { get; set; } = null!;
        public DateTime ScheduledDate { get; set; }
        public DateTime DepartureDateTime { get; set; }
        public DateTime ArrivalDateTime { get; set; }
        public bool HasNextDayArrival { get; set; }
    }
}
