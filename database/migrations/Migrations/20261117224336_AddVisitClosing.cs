using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "close_local_id",
                table: "paradas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "close_notes",
                table: "paradas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "paradas",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "closed_location",
                table: "paradas",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "garments_collected",
                table: "paradas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "no_realizada_reason_id",
                table: "paradas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cobros_referencia",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    parada_id = table.Column<int>(type: "int", nullable: false),
                    method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cobros_referencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_cobros_referencia_paradas_parada_id",
                        column: x => x.parada_id,
                        principalTable: "paradas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidencias",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    parada_id = table.Column<int>(type: "int", nullable: false),
                    kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    file_path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    taken_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    location = table.Column<Point>(type: "geography", nullable: true),
                    distance_meters = table.Column<int>(type: "int", nullable: true),
                    local_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evidencias", x => x.id);
                    table.ForeignKey(
                        name: "fk_evidencias_paradas_parada_id",
                        column: x => x.parada_id,
                        principalTable: "paradas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_paradas_no_realizada_reason_id",
                table: "paradas",
                column: "no_realizada_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobros_referencia_parada_id",
                table: "cobros_referencia",
                column: "parada_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidencias_local_id",
                table: "evidencias",
                column: "local_id",
                unique: true,
                filter: "[local_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_evidencias_parada_id_kind",
                table: "evidencias",
                columns: new[] { "parada_id", "kind" });

            migrationBuilder.AddForeignKey(
                name: "fk_paradas_reasons_no_realizada_reason_id",
                table: "paradas",
                column: "no_realizada_reason_id",
                principalTable: "reasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_paradas_reasons_no_realizada_reason_id",
                table: "paradas");

            migrationBuilder.DropTable(
                name: "cobros_referencia");

            migrationBuilder.DropTable(
                name: "evidencias");

            migrationBuilder.DropIndex(
                name: "ix_paradas_no_realizada_reason_id",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "close_local_id",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "close_notes",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "closed_location",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "garments_collected",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "no_realizada_reason_id",
                table: "paradas");
        }
    }
}
