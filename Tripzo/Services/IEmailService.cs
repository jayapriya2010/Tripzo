namespace Tripzo.Services;

public interface IEmailService
{
    Task SendTicketEmailAsync(string toEmail, string passengerName, byte[] pdfAttachment, int bookingId);
}
