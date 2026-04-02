namespace Tripzo.DTOs.Operator
{
    public class RouteCreateDTO
    {
        public int BusId { get; set; }
        public string SourceCity { get; set; }
        public string DestCity { get; set; }
        public decimal BaseFare { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public List<StopDTO> Stops { get; set; } // Nested list for all journey points
    }
}