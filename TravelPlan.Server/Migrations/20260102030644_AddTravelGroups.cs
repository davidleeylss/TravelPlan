using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlan.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripUser");

            migrationBuilder.AddColumn<int>(
                name: "TravelGroupId",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TravelGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelGroupUser",
                columns: table => new
                {
                    GroupsId = table.Column<int>(type: "int", nullable: false),
                    MembersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelGroupUser", x => new { x.GroupsId, x.MembersId });
                    table.ForeignKey(
                        name: "FK_TravelGroupUser_TravelGroups_GroupsId",
                        column: x => x.GroupsId,
                        principalTable: "TravelGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelGroupUser_Users_MembersId",
                        column: x => x.MembersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TravelGroupId",
                table: "Trips",
                column: "TravelGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelGroupUser_MembersId",
                table: "TravelGroupUser",
                column: "MembersId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_TravelGroups_TravelGroupId",
                table: "Trips",
                column: "TravelGroupId",
                principalTable: "TravelGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_TravelGroups_TravelGroupId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "TravelGroupUser");

            migrationBuilder.DropTable(
                name: "TravelGroups");

            migrationBuilder.DropIndex(
                name: "IX_Trips_TravelGroupId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TravelGroupId",
                table: "Trips");

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
                        name: "FK_TripUser_Trips_TripsId",
                        column: x => x.TripsId,
                        principalTable: "Trips",
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
                name: "IX_TripUser_TripsId",
                table: "TripUser",
                column: "TripsId");
        }
    }
}
