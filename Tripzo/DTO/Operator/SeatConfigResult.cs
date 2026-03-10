namespace Tripzo.DTOs.Operator;

public class SeatConfigResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string>? ConflictingSeatNumbers { get; set; }

    public static SeatConfigResult Ok() => new() { Success = true };

    public static SeatConfigResult Fail(string message) => new() { Success = false, ErrorMessage = message };

    public static SeatConfigResult DuplicateSeats(List<string> seatNumbers) => new()
    {
        Success = false,
        ErrorMessage = "Duplicate seat numbers in request",
        ConflictingSeatNumbers = seatNumbers
    };

    public static SeatConfigResult SeatsAlreadyExist(List<string> seatNumbers) => new()
    {
        Success = false,
        ErrorMessage = "Seat numbers already exist for this bus",
        ConflictingSeatNumbers = seatNumbers
    };
}
