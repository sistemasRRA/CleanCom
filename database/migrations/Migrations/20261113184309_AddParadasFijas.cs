using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddParadasFijas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "activity",
                table: "paradas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "paradas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "location",
                table: "paradas",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parada_fija_id",
                table: "paradas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "placement",
                table: "paradas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Automatica");

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "paradas",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "viaje_id",
                table: "paradas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "viaje_step",
                table: "paradas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "paradas_fijas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ruta_id = table.Column<int>(type: "int", nullable: false),
                    turno = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    placement = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    carried = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    site_contact = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    site_phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    garments_approx = table.Column<int>(type: "int", nullable: true),
                    driver_notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    paused = table.Column<bool>(type: "bit", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    days = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paradas_fijas", x => x.id);
                    table.ForeignKey(
                        name: "fk_paradas_fijas_rutas_ruta_id",
                        column: x => x.ruta_id,
                        principalTable: "rutas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parada_fija_exceptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parada_fija_id = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parada_fija_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_parada_fija_exceptions_paradas_fijas_parada_fija_id",
                        column: x => x.parada_fija_id,
                        principalTable: "paradas_fijas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parada_fija_pasos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    parada_fija_id = table.Column<int>(type: "int", nullable: false),
                    order = table.Column<int>(type: "int", nullable: false),
                    lugar_id = table.Column<int>(type: "int", nullable: true),
                    address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    location = table.Column<Point>(type: "geography", nullable: true),
                    activity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parada_fija_pasos", x => x.id);
                    table.ForeignKey(
                        name: "fk_parada_fija_pasos_lugares_lugar_id",
                        column: x => x.lugar_id,
                        principalTable: "lugares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_parada_fija_pasos_paradas_fijas_parada_fija_id",
                        column: x => x.parada_fija_id,
                        principalTable: "paradas_fijas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "viajes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ruta_dia_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    carried = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    parada_fija_id = table.Column<int>(type: "int", nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_viajes", x => x.id);
                    table.ForeignKey(
                        name: "fk_viajes_paradas_fijas_parada_fija_id",
                        column: x => x.parada_fija_id,
                        principalTable: "paradas_fijas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_viajes_ruta_dias_ruta_dia_id",
                        column: x => x.ruta_dia_id,
                        principalTable: "ruta_dias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_paradas_parada_fija_id_ruta_dia_id",
                table: "paradas",
                columns: new[] { "parada_fija_id", "ruta_dia_id" });

            migrationBuilder.CreateIndex(
                name: "ix_paradas_viaje_id",
                table: "paradas",
                column: "viaje_id");

            migrationBuilder.CreateIndex(
                name: "ix_parada_fija_exceptions_parada_fija_id_date",
                table: "parada_fija_exceptions",
                columns: new[] { "parada_fija_id", "date" },
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_parada_fija_pasos_lugar_id",
                table: "parada_fija_pasos",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "ix_parada_fija_pasos_parada_fija_id_order",
                table: "parada_fija_pasos",
                columns: new[] { "parada_fija_id", "order" },
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_paradas_fijas_name",
                table: "paradas_fijas",
                column: "name",
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_paradas_fijas_ruta_id",
                table: "paradas_fijas",
                column: "ruta_id");

            migrationBuilder.CreateIndex(
                name: "ix_viajes_parada_fija_id",
                table: "viajes",
                column: "parada_fija_id");

            migrationBuilder.CreateIndex(
                name: "ix_viajes_ruta_dia_id",
                table: "viajes",
                column: "ruta_dia_id");

            migrationBuilder.AddForeignKey(
                name: "fk_paradas_paradas_fijas_parada_fija_id",
                table: "paradas",
                column: "parada_fija_id",
                principalTable: "paradas_fijas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_paradas_viajes_viaje_id",
                table: "paradas",
                column: "viaje_id",
                principalTable: "viajes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_paradas_paradas_fijas_parada_fija_id",
                table: "paradas");

            migrationBuilder.DropForeignKey(
                name: "fk_paradas_viajes_viaje_id",
                table: "paradas");

            migrationBuilder.DropTable(
                name: "parada_fija_exceptions");

            migrationBuilder.DropTable(
                name: "parada_fija_pasos");

            migrationBuilder.DropTable(
                name: "viajes");

            migrationBuilder.DropTable(
                name: "paradas_fijas");

            migrationBuilder.DropIndex(
                name: "ix_paradas_parada_fija_id_ruta_dia_id",
                table: "paradas");

            migrationBuilder.DropIndex(
                name: "ix_paradas_viaje_id",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "activity",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "address",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "location",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "parada_fija_id",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "placement",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "title",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "viaje_id",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "viaje_step",
                table: "paradas");
        }
    }
}
