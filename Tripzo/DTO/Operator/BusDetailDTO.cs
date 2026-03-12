namespace Tripzo.DTOs.Operator
{
    public class BusDetailDTO
    {
        public int BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
        public List<string> Amenities { get; set; } = new();
        public List<BusSeatDetailDTO> Seats { get; set; } = new();
        public List<BusRouteDetailDTO> Routes { get; set; } = new();
        public BusOccupancySummaryDTO OccupancySummary { get; set; } = new();
    }

    public class BusSeatDetailDTO
    {
        public int SeatId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatType { get; set; } = string.Empty;
        public decimal AddonFare { get; set; }
    }

    public class BusRouteDetailDTO
    {
        public int RouteId { get; set; }
        public string SourceCity { get; set; } = string.Empty;
        public string DestCity { get; set; } = string.Empty;
        public decimal BaseFare { get; set; }
        public List<RouteStopDetailDTO> Stops { get; set; } = new();
    }

    public class RouteStopDetailDTO
    {
        public int StopId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string StopType { get; set; } = string.Empty;
        public int StopOrder { get; set; }
        public TimeSpan ArrivalTime { get; set; }
    }

    public class BusOccupancySummaryDTO
    {
        public int TotalSeats { get; set; }
        public List<ScheduleOccupancyDTO> UpcomingSchedules { get; set; } = new();
    }

    public class ScheduleOccupancyDTO
    {
        public int ScheduleId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public int OccupiedSeats { get; set; }
        public int AvailableSeats { get; set; }
        public double OccupancyPercentage { get; set; }
    }
}
