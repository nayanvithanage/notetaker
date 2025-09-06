using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notetaker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToRecallBotId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Meetings_RecallBotId",
                table: "Meetings",
                column: "RecallBotId",
                unique: true,
                filter: "\"RecallBotId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Meetings_RecallBotId",
                table: "Meetings");
        }
    }
}
