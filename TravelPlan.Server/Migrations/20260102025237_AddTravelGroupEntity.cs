using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlan.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelGroupEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropForeignKey(
                name: "FK_TripUser_Trip_TripsId",
                table: "TripUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trip",
                table: "Trip");

            migrationBuilder.RenameTable(
                name: "Trip",
                newName: "Trips");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trips",
                table: "Trips",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Trips_TripId",
                table: "Expenses",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Flights_Trips_TripId",
                table: "Flights",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItineraryItems_Trips_TripId",
                table: "ItineraryItems",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripUser_Trips_TripsId",
                table: "TripUser",
                column: "TripsId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Trips_TripId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Flights_Trips_TripId",
                table: "Flights");

            migrationBuilder.DropForeignKey(
                name: "FK_ItineraryItems_Trips_TripId",
                table: "ItineraryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TripUser_Trips_TripsId",
                table: "TripUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trips",
                table: "Trips");

            migrationBuilder.RenameTable(
                name: "Trips",
                newName: "Trip");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trip",
                table: "Trip",
                column: "Id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_TripUser_Trip_TripsId",
                table: "TripUser",
                column: "TripsId",
                principalTable: "Trip",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
