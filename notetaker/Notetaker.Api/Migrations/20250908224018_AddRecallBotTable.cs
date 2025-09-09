using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Notetaker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecallBotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecallBots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BotId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MeetingId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BotName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    JoinAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HasTranscript = table.Column<bool>(type: "boolean", nullable: false),
                    HasRecording = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecordingDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    StatusChangesJson = table.Column<string>(type: "jsonb", nullable: true),
                    RecordingsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RecordingConfigJson = table.Column<string>(type: "jsonb", nullable: true),
                    AutomaticLeaveJson = table.Column<string>(type: "jsonb", nullable: true),
                    CalendarMeetingsJson = table.Column<string>(type: "jsonb", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecallBots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecallBots_BotId",
                table: "RecallBots",
                column: "BotId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecallBots");
        }
    }
}
