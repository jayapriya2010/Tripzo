namespace Tripzo.Services;

public interface IEmailService
{
    Task SendTicketEmailAsync(string toEmail, string passengerName, byte[] pdfAttachment, int bookingId);
    Task SendCancellationRequestEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, DateTime journeyDate, decimal amount);
    Task SendCancellationApprovedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, decimal amount);
    Task SendCancellationRejectedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, DateTime journeyDate, decimal amount);
    Task SendRefundInitiatedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, decimal refundAmount);
    Task SendBusReassignmentEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, DateTime journeyDate, string oldBus, string newBus, byte[] pdfAttachment);
}
