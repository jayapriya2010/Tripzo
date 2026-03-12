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

        await SendEmailAsync(email);
    }

    public async Task SendCancellationRequestEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, DateTime journeyDate, decimal amount)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            _config["Email:SenderName"] ?? "Tripzo",
            _config["Email:SenderEmail"]));
        email.To.Add(new MailboxAddress(passengerName, toEmail));
        email.Subject = $"Cancellation Request Received - Booking #{bookingId}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <h2 style="color: #333;">Hi {passengerName},</h2>

                    <p>We have received your cancellation request for the following booking:</p>

                    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;">
                        <p><strong>Booking ID:</strong> #{bookingId}</p>
                        <p><strong>Route:</strong> {routeName}</p>
                        <p><strong>Journey Date:</strong> {journeyDate:dddd, MMMM dd, yyyy}</p>
                        <p><strong>Amount:</strong> ₹{amount:N2}</p>
                    </div>

                    <div style="background-color: #fff3cd; padding: 15px; border-radius: 5px; border-left: 4px solid #ffc107;">
                        <p style="margin: 0; color: #856404;">
                            <strong>⏳ Status: Under Review</strong>
                        </p>
                        <p style="margin: 10px 0 0 0; color: #856404;">
                            Your cancellation request is currently being reviewed by our team. 
                            You will receive an email once the cancellation is approved.
                        </p>
                    </div>

                    <br/>
                    <p>Thank you for your patience.</p>
                    <p>– The Tripzo Team</p>
                </div>
                """
        };

        email.Body = builder.ToMessageBody();

        await SendEmailAsync(email);
    }

    public async Task SendCancellationApprovedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, decimal amount)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            _config["Email:SenderName"] ?? "Tripzo",
            _config["Email:SenderEmail"]));
        email.To.Add(new MailboxAddress(passengerName, toEmail));
        email.Subject = $"Cancellation Approved - Booking #{bookingId}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <h2 style="color: #333;">Hi {passengerName},</h2>

                    <p>Good news! Your cancellation request has been <strong>approved</strong>.</p>

                    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;">
                        <p><strong>Booking ID:</strong> #{bookingId}</p>
                        <p><strong>Route:</strong> {routeName}</p>
                        <p><strong>Refund Amount:</strong> ₹{amount:N2}</p>
                    </div>

                    <div style="background-color: #d4edda; padding: 15px; border-radius: 5px; border-left: 4px solid #28a745;">
                        <p style="margin: 0; color: #155724;">
                            <strong>✅ Cancellation Approved</strong>
                        </p>
                        <p style="margin: 10px 0 0 0; color: #155724;">
                            Your refund will be initiated by the bus operator soon. 
                            You will receive another email once the refund is processed.
                        </p>
                    </div>

                    <br/>
                    <p>Thank you for choosing Tripzo.</p>
                    <p>– The Tripzo Team</p>
                </div>
                """
        };

        email.Body = builder.ToMessageBody();

        await SendEmailAsync(email);
    }

    public async Task SendCancellationRejectedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, DateTime journeyDate, decimal amount)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            _config["Email:SenderName"] ?? "Tripzo",
            _config["Email:SenderEmail"]));
        email.To.Add(new MailboxAddress(passengerName, toEmail));
        email.Subject = $"Cancellation Request Rejected - Booking #{bookingId}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <h2 style="color: #333;">Hi {passengerName},</h2>

                    <p>We regret to inform you that your cancellation request has been <strong>rejected</strong>.</p>

                    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;">
                        <p><strong>Booking ID:</strong> #{bookingId}</p>
                        <p><strong>Route:</strong> {routeName}</p>
                        <p><strong>Journey Date:</strong> {journeyDate:dddd, MMMM dd, yyyy}</p>
                        <p><strong>Amount:</strong> ₹{amount:N2}</p>
                    </div>

                    <div style="background-color: #f8d7da; padding: 15px; border-radius: 5px; border-left: 4px solid #dc3545;">
                        <p style="margin: 0; color: #721c24;">
                            <strong>❌ Cancellation Rejected</strong>
                        </p>
                        <p style="margin: 10px 0 0 0; color: #721c24;">
                            Your booking has been restored to <strong>Confirmed</strong> status. 
                            Your ticket remains valid for the scheduled journey.
                        </p>
                    </div>

                    <div style="background-color: #fff3cd; padding: 15px; border-radius: 5px; margin-top: 15px; border-left: 4px solid #ffc107;">
                        <p style="margin: 0; color: #856404;">
                            <strong>ℹ️ Why was my cancellation rejected?</strong>
                        </p>
                        <p style="margin: 10px 0 0 0; color: #856404;">
                            Cancellation requests may be rejected due to policy violations, 
                            last-minute cancellations, or other terms and conditions. 
                            Please contact our support team for more details.
                        </p>
                    </div>

                    <br/>
                    <p>If you have any questions, please contact our support team.</p>
                    <p>– The Tripzo Team</p>
                </div>
                """
        };

        email.Body = builder.ToMessageBody();

        await SendEmailAsync(email);
    }

    public async Task SendRefundInitiatedEmailAsync(string toEmail, string passengerName, int bookingId, string routeName, decimal refundAmount)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            _config["Email:SenderName"] ?? "Tripzo",
            _config["Email:SenderEmail"]));
        email.To.Add(new MailboxAddress(passengerName, toEmail));
        email.Subject = $"Refund Initiated - Booking #{bookingId}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <h2 style="color: #333;">Hi {passengerName},</h2>

                    <p>Great news! Your refund has been <strong>successfully initiated</strong>.</p>

                    <div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;">
                        <p><strong>Booking ID:</strong> #{bookingId}</p>
                        <p><strong>Route:</strong> {routeName}</p>
                        <p><strong>Refund Amount:</strong> ₹{refundAmount:N2}</p>
                    </div>

                    <div style="background-color: #cce5ff; padding: 15px; border-radius: 5px; border-left: 4px solid #007bff;">
                        <p style="margin: 0; color: #004085;">
                            <strong>💰 Refund Initiated</strong>
                        </p>
                        <p style="margin: 10px 0 0 0; color: #004085;">
                            The refund amount of <strong>₹{refundAmount:N2}</strong> has been initiated to your original payment method.
                        </p>
                        <p style="margin: 10px 0 0 0; color: #004085;">
                            <strong>Please allow 2-3 business days</strong> for the amount to reflect in your account.
                        </p>
                    </div>

                    <br/>
                    <p>If you don't receive the refund within 5 business days, please contact our support team.</p>
                    <p>Thank you for your patience and for choosing Tripzo.</p>
                    <p>– The Tripzo Team</p>
                </div>
                """
        };

        email.Body = builder.ToMessageBody();

        await SendEmailAsync(email);
    }

    private async Task SendEmailAsync(MimeMessage email)
    {
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["Email:SmtpHost"] ?? "smtp.gmail.com",
            int.Parse(_config["Email:SmtpPort"] ?? "587"),
            SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(
            _config["Email:Username"],
            _config["Email:Password"]);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
