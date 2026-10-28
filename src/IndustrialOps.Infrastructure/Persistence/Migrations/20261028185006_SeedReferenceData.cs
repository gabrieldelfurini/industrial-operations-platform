using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IndustrialOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "id", "code", "created_at", "is_active", "name", "unit_of_measure", "updated_at" },
                values: new object[,]
                {
                    { new Guid("0199a1b2-0000-7000-8000-000000000001"), "PRD-001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Steel Bracket", "UN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0199a1b2-0000-7000-8000-000000000002"), "PRD-002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Aluminum Housing", "UN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0199a1b2-0000-7000-8000-000000000003"), "PRD-003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Hydraulic Fluid", "L", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "work_centers",
                columns: new[] { "id", "code", "created_at", "description", "is_active", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("0199a1b2-0000-7000-8000-000000000101"), "LINE-01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Final assembly of brackets and housings", true, "Assembly Line 1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0199a1b2-0000-7000-8000-000000000102"), "CNC-01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "5-axis CNC machining", true, "CNC Machining Cell", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0199a1b2-0000-7000-8000-000000000103"), "PAINT-01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Paint Booth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("0199a1b2-0000-7000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("0199a1b2-0000-7000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("0199a1b2-0000-7000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "work_centers",
                keyColumn: "id",
                keyValue: new Guid("0199a1b2-0000-7000-8000-000000000101"));

            migrationBuilder.DeleteData(
                table: "work_centers",
                keyColumn: "id",
                keyValue: new Guid("0199a1b2-0000-7000-8000-000000000102"));

            migrationBuilder.DeleteData(
                table: "work_centers",
                keyColumn: "id",
                keyValue: new Guid("0199a1b2-0000-7000-8000-000000000103"));
        }
    }
}
