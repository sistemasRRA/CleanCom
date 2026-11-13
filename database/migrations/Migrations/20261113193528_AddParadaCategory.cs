using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddParadaCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "paradas_fijas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ClienteFinal");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "paradas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "paradas_fijas");

            migrationBuilder.DropColumn(
                name: "category",
                table: "paradas");
        }
    }
}
