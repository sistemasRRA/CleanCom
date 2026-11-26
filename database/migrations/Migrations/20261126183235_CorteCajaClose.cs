using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class CorteCajaClose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "cortes_caja",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "closed_by",
                table: "cortes_caja",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "folio",
                table: "cortes_caja",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "office_notes",
                table: "cortes_caja",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "pos",
                table: "corte_caja_lines",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "registered",
                table: "corte_caja_lines",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_cortes_caja_folio",
                table: "cortes_caja",
                column: "folio",
                unique: true,
                filter: "[folio] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cortes_caja_folio",
                table: "cortes_caja");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "cortes_caja");

            migrationBuilder.DropColumn(
                name: "closed_by",
                table: "cortes_caja");

            migrationBuilder.DropColumn(
                name: "folio",
                table: "cortes_caja");

            migrationBuilder.DropColumn(
                name: "office_notes",
                table: "cortes_caja");

            migrationBuilder.DropColumn(
                name: "pos",
                table: "corte_caja_lines");

            migrationBuilder.DropColumn(
                name: "registered",
                table: "corte_caja_lines");
        }
    }
}
