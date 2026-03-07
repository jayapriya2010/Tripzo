namespace Tripzo.DTOs.Admin
{
    public class ErrorLogDTO
    {
        public int LogId { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
    }
}