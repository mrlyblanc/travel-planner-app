using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlannerApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "itineraries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itineraries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_itineraries_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_audit_logs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ItineraryId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_audit_logs_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_event_audit_logs_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ItineraryId = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    StartDateTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LocationAddress = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LocationLat = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    LocationLng = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_events_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_events_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_events_users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "itinerary_members",
                columns: table => new
                {
                    ItineraryId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itinerary_members", x => new { x.ItineraryId, x.UserId });
                    table.ForeignKey(
                        name: "FK_itinerary_members_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_itinerary_members_users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_itinerary_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_audit_logs_ChangedByUserId",
                table: "event_audit_logs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_event_audit_logs_EventId",
                table: "event_audit_logs",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_event_audit_logs_ItineraryId",
                table: "event_audit_logs",
                column: "ItineraryId");

            migrationBuilder.CreateIndex(
                name: "IX_events_CreatedById",
                table: "events",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_events_ItineraryId",
                table: "events",
                column: "ItineraryId");

            migrationBuilder.CreateIndex(
                name: "IX_events_UpdatedById",
                table: "events",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_itineraries_CreatedById",
                table: "itineraries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_itinerary_members_AddedByUserId",
                table: "itinerary_members",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_itinerary_members_UserId",
                table: "itinerary_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_audit_logs");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "itinerary_members");

            migrationBuilder.DropTable(
                name: "itineraries");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
