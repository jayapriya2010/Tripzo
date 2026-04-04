namespace Tripzo.Services;

public interface IEmailService
{
    Task SendTicketEmailAsync(string toEmail, string passengerName, byte[] pdfAttachment, int bookingId, string seatNumbers);
    Task SendCancellationRequestEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, DateTime journeyDate, decimal amount, string seatNumbers);
    Task SendCancellationApprovedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, decimal amount, string seatNumbers);
    Task SendCancellationRejectedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, DateTime journeyDate, decimal amount, string seatNumbers);
    Task SendRefundInitiatedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, decimal refundAmount, string seatNumbers, byte[]? pdfAttachment = null);
    Task SendBusReassignmentEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, DateTime journeyDate, string oldBus, string newBus, byte[] pdfAttachment, string seatNumbers);
    Task SendOtpEmailAsync(string toEmail, string userName, string otp);
}
