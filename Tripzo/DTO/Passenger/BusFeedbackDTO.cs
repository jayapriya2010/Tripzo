namespace Tripzo.DTOs.Passenger
{
    public class BusFeedbackDTO
    {
        public int FeedbackId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime JourneyDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OperatorResponse { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class BusFeedbackSummaryDTO
    {
        public int BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
        public List<BusFeedbackDTO> Reviews { get; set; } = new();
    }
}
