using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlan.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToGroupArch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Owner",
                table: "Flights",
                newName: "Participants");

            migrationBuilder.AddColumn<string>(
                name: "Participants",
                table: "ItineraryItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Participants",
                table: "ItineraryItems");

            migrationBuilder.RenameColumn(
                name: "Participants",
                table: "Flights",
                newName: "Owner");
        }
    }
}
