using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripzo.Migrations
{
    /// <inheritdoc />
    public partial class reschedule_date_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DayOffset",
                table: "RouteStops",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayOffset",
                table: "RouteStops");
        }
    }
}
