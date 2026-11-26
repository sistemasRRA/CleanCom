using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddKpis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kpi_goals",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    indicador_key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    period = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    responsable_user_id = table.Column<int>(type: "int", nullable: true),
                    red = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    green = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    super_green = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kpi_goals", x => x.id);
                    table.ForeignKey(
                        name: "fk_kpi_goals_users_responsable_user_id",
                        column: x => x.responsable_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kpi_values",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    indicador_key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    period = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    ruta_id = table.Column<int>(type: "int", nullable: true),
                    value = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    calculated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kpi_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_kpi_values_rutas_ruta_id",
                        column: x => x.ruta_id,
                        principalTable: "rutas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_kpi_goals_indicador_key_valid_from",
                table: "kpi_goals",
                columns: new[] { "indicador_key", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "ix_kpi_goals_responsable_user_id",
                table: "kpi_goals",
                column: "responsable_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_kpi_values_period_period_start_indicador_key",
                table: "kpi_values",
                columns: new[] { "period", "period_start", "indicador_key" });

            migrationBuilder.CreateIndex(
                name: "ix_kpi_values_ruta_id",
                table: "kpi_values",
                column: "ruta_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kpi_goals");

            migrationBuilder.DropTable(
                name: "kpi_values");
        }
    }
}
