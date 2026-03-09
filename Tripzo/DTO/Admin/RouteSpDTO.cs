namespace Tripzo.DTO.Admin
{
    public class RouteSpDTO
    {
        public int RouteId { get; set; }
        public string SourceCity { get; set; }
        public string DestCity { get; set; }
        public decimal BaseFare { get; set; }
        public string? BusName { get; set; }
        public string? BusNumber { get; set; }
    }
}
