namespace Tripzo.DTO.Admin
{
    public class RouteDetailsDTO
    {
        public int RouteId { get; set; }
        public string BusName { get; set; }
        public string BusNumber { get; set; }
        public string SourceCity { get; set; }
        public string DestCity { get; set; }
        public decimal BaseFare { get; set; }
        public List<RouteStopDetailsDTO> Stops { get; set; }
    }

    public class RouteStopDetailsDTO
    {
        public int StopId { get; set; }
        public string CityName { get; set; }
        public string LocationName { get; set; }
        public string StopType { get; set; }
        public int StopOrder { get; set; }
        public TimeSpan ArrivalTime { get; set; }
    }
}
