namespace Tripzo.DTOs.Admin
{
    public class AdminDashboardDTO
    {
        public int TotalPassengers { get; set; }
        public int ActiveOperators { get; set; }
        public int TotalBuses { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TodaysBookings { get; set; }
        public int SystemErrorsLast24Hours { get; set; }
    }
}