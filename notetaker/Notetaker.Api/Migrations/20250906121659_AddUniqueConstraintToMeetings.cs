using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notetaker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToMeetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Meetings_UserId",
                table: "Meetings");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_UserId_CalendarEventId",
                table: "Meetings",
                columns: new[] { "UserId", "CalendarEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Meetings_UserId_CalendarEventId",
                table: "Meetings");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_UserId",
                table: "Meetings",
                column: "UserId");
        }
    }
}
