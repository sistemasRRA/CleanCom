using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddChoferLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chofer_locations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ruta_dia_id = table.Column<int>(type: "int", nullable: false),
                    location = table.Column<Point>(type: "geography", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    accuracy_meters = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chofer_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_chofer_locations_ruta_dias_ruta_dia_id",
                        column: x => x.ruta_dia_id,
                        principalTable: "ruta_dias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "created_at", "created_by", "effective_from", "key", "modified_at", "modified_by", "note", "type", "value" },
                values: new object[,]
                {
                    { 33, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gps.HistorialSegundos", null, null, "Valor inicial", "Integer", "120" },
                    { 34, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gps.HistorialMetros", null, null, "Valor inicial", "Integer", "100" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_chofer_locations_ruta_dia_id_recorded_at",
                table: "chofer_locations",
                columns: new[] { "ruta_dia_id", "recorded_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chofer_locations");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 34);
        }
    }
}
