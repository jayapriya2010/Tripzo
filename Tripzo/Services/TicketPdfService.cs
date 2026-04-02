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
                            c.Item().Text($"{ticket.BusName} ({ticket.BusType}) - {ticket.BusNumber}");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Booked by").SemiBold().AlignRight();
                            c.Item().Text(ticket.PassengerName).AlignRight();
                        });
                    });

                    col.Item().PaddingTop(15).Text("Traveler Information").Bold().FontSize(14).Underline();
                    
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40); // Seat
                            columns.RelativeColumn();   // Name
                            columns.ConstantColumn(40); // Age
                            columns.ConstantColumn(60); // Gender
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).PaddingVertical(2).Text("Seat").Bold().FontSize(10);
                            header.Cell().BorderBottom(1).PaddingVertical(2).Text("Name").Bold().FontSize(10);
                            header.Cell().BorderBottom(1).PaddingVertical(2).Text("Age").Bold().FontSize(10);
                            header.Cell().BorderBottom(1).PaddingVertical(2).Text("Gender").Bold().FontSize(10);
                        });

                        foreach (var p in ticket.Passengers)
                        {
                            var seatNum = ticket.SeatNumbers.Count > ticket.Passengers.IndexOf(p) 
                                ? ticket.SeatNumbers[ticket.Passengers.IndexOf(p)] : "??";
                                
                            table.Cell().PaddingVertical(2).Text(seatNum).FontSize(10);
                            table.Cell().PaddingVertical(2).Text(p.Name).FontSize(10);
                            table.Cell().PaddingVertical(2).Text(p.Age.ToString()).FontSize(10);
                            table.Cell().PaddingVertical(2).Text(p.Gender).FontSize(10);
                        }
                    });

                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                           c.Item().Text("Total Amount").SemiBold();
                           c.Item().Text($"₹{ticket.TotalAmount:N2}").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        });
                        
                        row.RelativeItem().AlignRight().AlignMiddle().Text(text =>
                        {
                           text.Span("Status: ").SemiBold();
                           text.Span("CONFIRMED").Bold().FontColor(Colors.Green.Medium);
                        });
                    });
                });

                page.Footer().AlignCenter().Column(col =>
                {
                    col.Item().Text("Note: Please bring any govt. issued id for verification on the date of travel.")
                        .FontSize(9).FontColor(Colors.Red.Medium).Bold().AlignCenter();
                    col.Item().Text(text =>
                    {
                        text.Span($"Booked on {ticket.BookingDate:MMM dd, yyyy} | ");
                        text.Span("Thank you for choosing Tripzo!");
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
