using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripzo.Migrations
{
    /// <inheritdoc />
    public partial class bookingwithpassengerdetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "BookedSeats",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PassengerAge",
                table: "BookedSeats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PassengerName",
                table: "BookedSeats",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassengerPhone",
                table: "BookedSeats",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "BookedSeats");

            migrationBuilder.DropColumn(
                name: "PassengerAge",
                table: "BookedSeats");

            migrationBuilder.DropColumn(
                name: "PassengerName",
                table: "BookedSeats");

            migrationBuilder.DropColumn(
                name: "PassengerPhone",
                table: "BookedSeats");
        }
    }
}
