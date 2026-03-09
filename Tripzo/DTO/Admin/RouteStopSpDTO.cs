namespace Tripzo.DTO.Admin
{
    public class RouteStopSpDTO
    {
        public int StopId { get; set; }
        public int RouteId { get; set; }
        public string CityName { get; set; }
        public string LocationName { get; set; }
        public string StopType { get; set; }
        public int StopOrder { get; set; }
        public TimeSpan ArrivalTime { get; set; }
    }
}
