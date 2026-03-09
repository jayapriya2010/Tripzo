using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tripzo.DTOs.Passenger;

namespace Tripzo.Services;

public class TicketPdfService : ITicketPdfService
{
    public byte[] GenerateTicketPdf(TicketDTO ticket)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Text("Tripzo Bus Ticket")
                        .Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                    row.ConstantItem(100).Text($"#{ticket.BookingId}")
                        .AlignRight().FontSize(14);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Passenger").SemiBold();
                            c.Item().Text(ticket.PassengerName);
                            c.Item().Text(ticket.PassengerEmail).FontSize(10);
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Journey Date").SemiBold();
                            c.Item().Text(ticket.JourneyDate.ToString("dddd, MMMM dd, yyyy"));
                        });
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("From").SemiBold();
                            c.Item().Text(ticket.SourceCity).FontSize(16);
                        });

                        row.ConstantItem(50).AlignCenter().AlignMiddle()
                            .Text("→").FontSize(20);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("To").SemiBold();
                            c.Item().Text(ticket.DestCity).FontSize(16);
                        });
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bus").SemiBold();
                            c.Item().Text($"{ticket.BusName} ({ticket.BusNumber})");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Seat(s)").SemiBold();
                            c.Item().Text(string.Join(", ", ticket.SeatNumbers));
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Total Amount").SemiBold();
                            c.Item().Text($"₹{ticket.TotalAmount:N2}").FontSize(14).Bold();
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"Booked on {ticket.BookingDate:MMM dd, yyyy} | ");
                    text.Span("Thank you for choosing Tripzo!");
                });
            });
        });

        return document.GeneratePdf();
    }
}
