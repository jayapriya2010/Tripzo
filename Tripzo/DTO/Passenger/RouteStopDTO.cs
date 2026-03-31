using System;

namespace Tripzo.DTOs.Passenger
{
    public class RouteStopDTO
    {
        public int StopId { get; set; }
        public string? CityName { get; set; }
        public string? LocationName { get; set; }
        public TimeSpan ArrivalTime { get; set; }
    }
}
