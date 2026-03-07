namespace Tripzo.DTOs.Operator
{
    public class BusDTO
    {
        public int BusId { get; set; }
        public string BusName { get; set; }
        public string BusNumber { get; set; }
        public string BusType { get; set; } // AC, Non-AC, Sleeper
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
    }
}