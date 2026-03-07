namespace Tripzo.DTOs.Operator
{
    public class StopDTO
    {
        public string CityName { get; set; }
        public string LocationName { get; set; }
        public string StopType { get; set; } // "Boarding" or "Dropping"
        public int StopOrder { get; set; }
        public TimeSpan ArrivalTime { get; set; }
    }
}