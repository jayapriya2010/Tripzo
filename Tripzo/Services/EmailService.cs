using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Tripzo.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendTicketEmailAsync(string toEmail, string passengerName, byte[] pdfAttachment, int bookingId)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            _config["Email:SenderName"],
            _config["Email:SenderEmail"]));
        email.To.Add(new MailboxAddress(passengerName, toEmail));
        email.Subject = $"Your Tripzo Ticket - Booking #{bookingId}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <h2>Hi {passengerName},</h2>
                <p>Thank you for booking with Tripzo! Your ticket is confirmed.</p>
                <p>Please find your e-ticket attached to this email.</p>
                <br/>
                <p>Have a safe journey!</p>
                <p>– The Tripzo Team</p>
                """
        };

        builder.Attachments.Add($"Tripzo_Ticket_{bookingId}.pdf", pdfAttachment,
            new ContentType("application", "pdf"));

        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["Email:SmtpHost"],
            int.Parse(_config["Email:SmtpPort"]!),
            SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(
            _config["Email:Username"],
            _config["Email:Password"]);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
