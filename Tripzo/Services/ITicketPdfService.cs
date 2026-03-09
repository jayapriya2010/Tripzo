using Tripzo.DTOs.Passenger;

namespace Tripzo.Services;

public interface ITicketPdfService
{
    byte[] GenerateTicketPdf(TicketDTO ticket);
}
