using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddChoferEnCaminoLlegue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "arrived_at",
                table: "paradas",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "arrived_distance_meters",
                table: "paradas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "arrived_location",
                table: "paradas",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "arrived_outside_radius",
                table: "paradas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "en_camino_at",
                table: "paradas",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "arrived_at",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "arrived_distance_meters",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "arrived_location",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "arrived_outside_radius",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "en_camino_at",
                table: "paradas");
        }
    }
}
