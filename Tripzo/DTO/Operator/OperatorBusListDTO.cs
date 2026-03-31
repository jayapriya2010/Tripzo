namespace Tripzo.DTOs.Operator
{
    public class OperatorBusListDTO
    {
        public int BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
        public List<BusRouteDTO> Routes { get; set; } = new();
        public List<string> Amenities { get; set; } = new();
        public double AverageRating { get; set; }
        public int FeedbackCount { get; set; }
    }

    public class BusRouteDTO
    {
        public int RouteId { get; set; }
        public string SourceCity { get; set; } = string.Empty;
        public string DestCity { get; set; } = string.Empty;
        public decimal BaseFare { get; set; }
        public int TotalStops { get; set; }
    }
}
