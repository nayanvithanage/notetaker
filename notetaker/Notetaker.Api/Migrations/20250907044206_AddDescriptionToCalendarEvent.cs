using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notetaker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToCalendarEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CalendarEvents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "CalendarEvents");
        }
    }
}
