using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tripzo.Services
{
    public interface IRazorpayService
    {
        /// <summary>
        /// Creates a Razorpay order for the given amount
        /// </summary>
        Task<RazorpayOrderResult> CreateOrderAsync(decimal amount, string receiptId);

        /// <summary>
        /// Verifies the payment signature to ensure authenticity
        /// </summary>
        bool VerifyPaymentSignature(string orderId, string paymentId, string signature);

        /// <summary>
        /// Processes a refund through Razorpay
        /// </summary>
        Task<RazorpayRefundResult> ProcessRefundAsync(string paymentId, decimal amount);

        /// <summary>
        /// Returns the Razorpay Key ID for frontend initialization
        /// </summary>
        string GetKeyId();
    }

    public class RazorpayOrderResult
    {
        public bool Success { get; set; }
        public string? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string? ErrorMessage { get; set; }
    }

    public class RazorpayRefundResult
    {
        public bool Success { get; set; }
        public string? RefundId { get; set; }
        public decimal Amount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
