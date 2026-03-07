using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripzo.Migrations
{
    /// <inheritdoc />
    public partial class DBCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_UserId1",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Buses_BusId1",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_BusId1",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BusId1",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusId1",
                table: "Routes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_BusId1",
                table: "Routes",
                column: "BusId1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId1",
                table: "Bookings",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_UserId1",
                table: "Bookings",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Buses_BusId1",
                table: "Routes",
                column: "BusId1",
                principalTable: "Buses",
                principalColumn: "BusId");
        }
    }
}
