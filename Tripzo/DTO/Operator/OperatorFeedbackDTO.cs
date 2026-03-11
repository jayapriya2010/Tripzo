namespace Tripzo.DTOs.Operator
{
    public class OperatorFeedbackDTO
    {
        public int FeedbackId { get; set; }
        public int BookingId { get; set; }
        public int BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public DateTime JourneyDate { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OperatorResponse { get; set; }
        public DateTime? RespondedAt { get; set; }
        public bool HasResponded => !string.IsNullOrEmpty(OperatorResponse);
    }

    public class FeedbackResponseRequestDTO
    {
        public int FeedbackId { get; set; }
        public string Response { get; set; } = string.Empty;
    }

    public class OperatorFeedbackSummaryDTO
    {
        public int TotalFeedbacks { get; set; }
        public int PendingResponses { get; set; }
        public double AverageRating { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
    }
}
