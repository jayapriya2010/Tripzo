namespace Tripzo.DTOs.Operator
{
    public class OperatorDashboardDTO
    {
        public int TotalBuses { get; set; }
        public int TotalActiveRoutes { get; set; }
        public int BookingsToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public double AverageOccupancyRate { get; set; }
    }
}