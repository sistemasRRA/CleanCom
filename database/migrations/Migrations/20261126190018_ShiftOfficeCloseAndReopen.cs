using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ShiftOfficeCloseAndReopen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "shift_ended_by",
                table: "ruta_dias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reopen_reason",
                table: "cortes_caja",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reopened_at",
                table: "cortes_caja",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reopened_by",
                table: "cortes_caja",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "id", "created_at", "created_by", "level", "modified_at", "modified_by", "role_id", "screen" },
                values: new object[,]
                {
                    { 10001, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 1, "reabrir-cortes" },
                    { 10002, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 1, "cerrar-turnos" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 10001);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 10002);

            migrationBuilder.DropColumn(
                name: "shift_ended_by",
                table: "ruta_dias");

            migrationBuilder.DropColumn(
                name: "reopen_reason",
                table: "cortes_caja");

            migrationBuilder.DropColumn(
                name: "reopened_at",
                table: "cortes_caja");

            migrationBuilder.DropColumn(
                name: "reopened_by",
                table: "cortes_caja");
        }
    }
}
