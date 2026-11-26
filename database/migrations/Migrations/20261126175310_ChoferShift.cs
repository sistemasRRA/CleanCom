using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ChoferShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "local_id",
                table: "unidad_events",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "end_km",
                table: "ruta_dias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shift_end_local_id",
                table: "ruta_dias",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "shift_ended_at",
                table: "ruta_dias",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shift_start_local_id",
                table: "ruta_dias",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "shift_started_at",
                table: "ruta_dias",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "start_km",
                table: "ruta_dias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cortes_caja",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ruta_dia_id = table.Column<int>(type: "int", nullable: false),
                    operador_id = table.Column<int>(type: "int", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    chofer_notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cortes_caja", x => x.id);
                    table.ForeignKey(
                        name: "fk_cortes_caja_operadores_operador_id",
                        column: x => x.operador_id,
                        principalTable: "operadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cortes_caja_ruta_dias_ruta_dia_id",
                        column: x => x.ruta_dia_id,
                        principalTable: "ruta_dias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "corte_caja_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    corte_caja_id = table.Column<int>(type: "int", nullable: false),
                    method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    declared = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corte_caja_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_corte_caja_lines_cortes_caja_corte_caja_id",
                        column: x => x.corte_caja_id,
                        principalTable: "cortes_caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "created_at", "created_by", "effective_from", "key", "modified_at", "modified_by", "note", "type", "value" },
                values: new object[] { 46, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Unidad.KmMaximoPorTurno", null, null, "Valor inicial", "Integer", "400" });

            migrationBuilder.CreateIndex(
                name: "ix_unidad_events_local_id",
                table: "unidad_events",
                column: "local_id",
                unique: true,
                filter: "[local_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_corte_caja_lines_corte_caja_id_method",
                table: "corte_caja_lines",
                columns: new[] { "corte_caja_id", "method" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cortes_caja_date",
                table: "cortes_caja",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_cortes_caja_operador_id",
                table: "cortes_caja",
                column: "operador_id");

            migrationBuilder.CreateIndex(
                name: "ix_cortes_caja_ruta_dia_id",
                table: "cortes_caja",
                column: "ruta_dia_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "corte_caja_lines");

            migrationBuilder.DropTable(
                name: "cortes_caja");

            migrationBuilder.DropIndex(
                name: "ix_unidad_events_local_id",
                table: "unidad_events");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 46);

            migrationBuilder.DropColumn(
                name: "local_id",
                table: "unidad_events");

            migrationBuilder.DropColumn(
                name: "end_km",
                table: "ruta_dias");

            migrationBuilder.DropColumn(
                name: "shift_end_local_id",
                table: "ruta_dias");

            migrationBuilder.DropColumn(
                name: "shift_ended_at",
                table: "ruta_dias");

            migrationBuilder.DropColumn(
                name: "shift_start_local_id",
                table: "ruta_dias");

            migrationBuilder.DropColumn(
                name: "shift_started_at",
                table: "ruta_dias");

            migrationBuilder.DropColumn(
                name: "start_km",
                table: "ruta_dias");
        }
    }
}
