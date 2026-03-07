using System.ComponentModel.DataAnnotations;

namespace Tripzo.Models
{
    public class ErrorLog
    {
        public int LogId { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
