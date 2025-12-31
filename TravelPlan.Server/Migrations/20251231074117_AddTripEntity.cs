using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlan.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTripEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "ItineraryItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "Flights",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "Expenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Trip",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trip", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TripUser",
                columns: table => new
                {
                    ParticipantsId = table.Column<int>(type: "int", nullable: false),
                    TripsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripUser", x => new { x.ParticipantsId, x.TripsId });
                    table.ForeignKey(
                        name: "FK_TripUser_Trip_TripsId",
                        column: x => x.TripsId,
                        principalTable: "Trip",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripUser_Users_ParticipantsId",
                        column: x => x.ParticipantsId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryItems_TripId",
                table: "ItineraryItems",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_TripId",
                table: "Flights",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TripId",
                table: "Expenses",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripUser_TripsId",
                table: "TripUser",
                column: "TripsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Trip_TripId",
                table: "Expenses",
                column: "TripId",
                principalTable: "Trip",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Flights_Trip_TripId",
                table: "Flights",
                column: "TripId",
                principalTable: "Trip",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItineraryItems_Trip_TripId",
                table: "ItineraryItems",
                column: "TripId",
                principalTable: "Trip",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Trip_TripId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Flights_Trip_TripId",
                table: "Flights");

            migrationBuilder.DropForeignKey(
                name: "FK_ItineraryItems_Trip_TripId",
                table: "ItineraryItems");

            migrationBuilder.DropTable(
                name: "TripUser");

            migrationBuilder.DropTable(
                name: "Trip");

            migrationBuilder.DropIndex(
                name: "IX_ItineraryItems_TripId",
                table: "ItineraryItems");

            migrationBuilder.DropIndex(
                name: "IX_Flights_TripId",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_TripId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Expenses");
        }
    }
}
