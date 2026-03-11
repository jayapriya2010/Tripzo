using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripzo.Migrations
{
    /// <inheritdoc />
    public partial class feedbackupdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusId",
                table: "Feedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OperatorResponse",
                table: "Feedbacks",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "Feedbacks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_BusId",
                table: "Feedbacks",
                column: "BusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Buses_BusId",
                table: "Feedbacks",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "BusId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Buses_BusId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_BusId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "BusId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "OperatorResponse",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "Feedbacks");
        }
    }
}
