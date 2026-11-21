using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadTypeAndPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "unidades",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Otro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "unidades");
        }
    }
}
