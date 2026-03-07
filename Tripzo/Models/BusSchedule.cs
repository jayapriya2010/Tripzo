namespace Tripzo.Models
{
    public class BusSchedule
    {
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Route Route { get; set; } = null!;
        public Bus Bus { get; set; } = null!;
    }
}