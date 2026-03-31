using Tripzo.DTO.Admin;

namespace Tripzo.DTOs.Operator
{
    public class OperatorRouteDetailDTO
    {
        public int RouteId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public string SourceCity { get; set; } = string.Empty;
        public string DestCity { get; set; } = string.Empty;
        public decimal BaseFare { get; set; }
        public List<RouteStopDetailsDTO> Stops { get; set; } = new();
        
        // Metrics
        public int ActiveBookingsCount { get; set; }
    }
}
