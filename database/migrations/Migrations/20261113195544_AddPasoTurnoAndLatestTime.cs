using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPasoTurnoAndLatestTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "before",
                table: "paradas",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "before",
                table: "parada_fija_pasos",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "parada_fija_pasos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "turno",
                table: "parada_fija_pasos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "before",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "before",
                table: "parada_fija_pasos");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "parada_fija_pasos");

            migrationBuilder.DropColumn(
                name: "turno",
                table: "parada_fija_pasos");
        }
    }
}
