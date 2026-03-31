using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tripzo.Services
{
    public class RazorpayService : IRazorpayService
    {
        private readonly string _keyId;
        private readonly string _keySecret;
        private readonly HttpClient _httpClient;
        private const string RazorpayBaseUrl = "https://api.razorpay.com/v1";

        public RazorpayService(IConfiguration configuration)
        {
            _keyId = configuration["Razorpay:KeyId"]
                ?? throw new ArgumentNullException("Razorpay:KeyId is not configured");
            _keySecret = configuration["Razorpay:KeySecret"]
                ?? throw new ArgumentNullException("Razorpay:KeySecret is not configured");

            _httpClient = new HttpClient();

            // Razorpay uses Basic Auth: key_id:key_secret
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        public string GetKeyId() => _keyId;

        /// <summary>
        /// Creates a Razorpay order. Amount is in rupees, converted to paise internally.
        /// </summary>
        public async Task<RazorpayOrderResult> CreateOrderAsync(decimal amount, string receiptId)
        {
            try
            {
                var orderRequest = new
                {
                    amount = (int)(amount * 100), // Razorpay expects amount in paise
                    currency = "INR",
                    receipt = receiptId,
                    payment_capture = 1 // Auto-capture payment
                };

                var json = JsonSerializer.Serialize(orderRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{RazorpayBaseUrl}/orders", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new RazorpayOrderResult
                    {
                        Success = false,
                        ErrorMessage = $"Razorpay order creation failed: {responseBody}"
                    };
                }

                var orderResponse = JsonSerializer.Deserialize<RazorpayOrderResponse>(responseBody);

                return new RazorpayOrderResult
                {
                    Success = true,
                    OrderId = orderResponse?.Id,
                    Amount = amount,
                    Currency = "INR"
                };
            }
            catch (Exception ex)
            {
                return new RazorpayOrderResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create Razorpay order: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Verifies payment authenticity using HMAC-SHA256 signature.
        /// Razorpay signs: "orderId|paymentId" with your key_secret.
        /// </summary>
        public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
        {
            try
            {
                var payload = $"{orderId}|{paymentId}";
                var keyBytes = Encoding.UTF8.GetBytes(_keySecret);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                using var hmac = new HMACSHA256(keyBytes);
                var computedHash = hmac.ComputeHash(payloadBytes);
                var computedSignature = BitConverter.ToString(computedHash).Replace("-", "").ToLowerInvariant();

                return string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Processes a refund through Razorpay's Refund API.
        /// Amount is in rupees, converted to paise internally.
        /// </summary>
        public async Task<RazorpayRefundResult> ProcessRefundAsync(string paymentId, decimal amount)
        {
            try
            {
                var refundRequest = new
                {
                    amount = (int)(amount * 100) // Paise
                };

                var json = JsonSerializer.Serialize(refundRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{RazorpayBaseUrl}/payments/{paymentId}/refund", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new RazorpayRefundResult
                    {
                        Success = false,
                        ErrorMessage = $"Razorpay refund failed: {responseBody}"
                    };
                }

                var refundResponse = JsonSerializer.Deserialize<RazorpayRefundResponse>(responseBody);

                return new RazorpayRefundResult
                {
                    Success = true,
                    RefundId = refundResponse?.Id,
                    Amount = amount
                };
            }
            catch (Exception ex)
            {
                return new RazorpayRefundResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to process refund: {ex.Message}"
                };
            }
        }

        // Internal models for deserializing Razorpay API responses
        private class RazorpayOrderResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("amount")]
            public int Amount { get; set; }

            [JsonPropertyName("currency")]
            public string? Currency { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }
        }

        private class RazorpayRefundResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("amount")]
            public int Amount { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }
        }
    }
}
