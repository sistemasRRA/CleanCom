using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiGoalRutaAndOptionalThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "super_green",
                table: "kpi_goals",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "red",
                table: "kpi_goals",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AddColumn<int>(
                name: "ruta_id",
                table: "kpi_goals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_kpi_goals_ruta_id",
                table: "kpi_goals",
                column: "ruta_id");

            migrationBuilder.AddForeignKey(
                name: "fk_kpi_goals_rutas_ruta_id",
                table: "kpi_goals",
                column: "ruta_id",
                principalTable: "rutas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_kpi_goals_rutas_ruta_id",
                table: "kpi_goals");

            migrationBuilder.DropIndex(
                name: "ix_kpi_goals_ruta_id",
                table: "kpi_goals");

            migrationBuilder.DropColumn(
                name: "ruta_id",
                table: "kpi_goals");

            migrationBuilder.AlterColumn<decimal>(
                name: "super_green",
                table: "kpi_goals",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(14,2)",
                oldPrecision: 14,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "red",
                table: "kpi_goals",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(14,2)",
                oldPrecision: 14,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
