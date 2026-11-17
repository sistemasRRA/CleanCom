using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRutaPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "position_fixed",
                table: "paradas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "created_at", "created_by", "effective_from", "key", "modified_at", "modified_by", "note", "type", "value" },
                values: new object[,]
                {
                    { 30, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ruta.VelocidadPromedio", null, null, "Valor inicial", "Decimal", "18" },
                    { 31, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ruta.FactorDistancia", null, null, "Valor inicial", "Decimal", "1.4" },
                    { 32, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Parada.VentanaHoraEspecifica", null, null, "Valor inicial", "Integer", "30" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 32);

            migrationBuilder.DropColumn(
                name: "position_fixed",
                table: "paradas");
        }
    }
}
