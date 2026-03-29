using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlannerApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimisticConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyToken",
                table: "users",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE users SET ConcurrencyToken = REPLACE(UUID(), '-', '') WHERE ConcurrencyToken = '';");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyToken",
                table: "itineraries",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE itineraries SET ConcurrencyToken = REPLACE(UUID(), '-', '') WHERE ConcurrencyToken = '';");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyToken",
                table: "events",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE events SET ConcurrencyToken = REPLACE(UUID(), '-', '') WHERE ConcurrencyToken = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "events");
        }
    }
}
