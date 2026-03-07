namespace Tripzo.DTOs.Operator
{
    public class BusCreateDTO
    {
        public string BusName { get; set; }
        public string BusNumber { get; set; }
        public string BusType { get; set; }
        public int Capacity { get; set; }
        public int OperatorId { get; set; }
    }
}