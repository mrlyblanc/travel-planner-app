using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlannerApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItineraryShareCodesAndUserNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShareCode",
                table: "itineraries",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShareCodeUpdatedAtUtc",
                table: "itineraries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                """
                ;WITH numbered_itineraries AS (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [RowNumber]
                    FROM [itineraries]
                )
                UPDATE itinerary
                SET
                    [ShareCode] = RIGHT('00000' + CAST(10000 + numbered.[RowNumber] AS varchar(10)), 5),
                    [ShareCodeUpdatedAtUtc] = CASE
                        WHEN itinerary.[UpdatedAtUtc] > '1900-01-01T00:00:00'
                            THEN itinerary.[UpdatedAtUtc]
                        ELSE SYSUTCDATETIME()
                    END
                FROM [itineraries] AS itinerary
                INNER JOIN numbered_itineraries AS numbered ON numbered.[Id] = itinerary.[Id]
                WHERE itinerary.[ShareCode] IS NULL OR LTRIM(RTRIM(itinerary.[ShareCode])) = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ShareCode",
                table: "itineraries",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ItineraryId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ActorUserId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_notifications_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_notifications_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_itineraries_ShareCode",
                table: "itineraries",
                column: "ShareCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_ActorUserId",
                table: "user_notifications",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_ItineraryId",
                table: "user_notifications",
                column: "ItineraryId");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_UserId_CreatedAtUtc",
                table: "user_notifications",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropIndex(
                name: "IX_itineraries_ShareCode",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "ShareCode",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "ShareCodeUpdatedAtUtc",
                table: "itineraries");
        }
    }
}
