using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripzo.Migrations
{
    /// <inheritdoc />
    public partial class PartialCancellationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "BookedSeats",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BookedSeats",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Confirmed");

            // Data Migration: Map existing Booking status to BookedSeat status
            migrationBuilder.Sql(@"
                -- Map 'Cancelled' (Pending Review) bookings
                UPDATE BookedSeats 
                SET Status = 'CancellationPending' 
                WHERE BookingId IN (SELECT BookingId FROM Bookings WHERE Status = 'Cancelled');

                -- Map 'CancellationApproved' (Pending Refund) bookings
                UPDATE BookedSeats 
                SET Status = 'CancellationApproved' 
                WHERE BookingId IN (SELECT BookingId FROM Bookings WHERE Status = 'CancellationApproved');

                -- Everything else is Confirmed (handled by default value but being explicit here)
                UPDATE BookedSeats SET Status = 'Confirmed' WHERE Status = '';
            ");

            // Migration: Move existing CancellationReason from Booking to BookedSeats for any matching cancellation
            migrationBuilder.Sql(@"
                UPDATE bs
                SET bs.CancellationReason = b.CancellationReason
                FROM BookedSeats bs
                JOIN Bookings b ON bs.BookingId = b.BookingId
                WHERE bs.Status IN ('CancellationPending', 'CancellationApproved');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "BookedSeats");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BookedSeats");
        }
    }
}
