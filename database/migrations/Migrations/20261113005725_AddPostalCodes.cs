using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPostalCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "postal_codes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nchar(5)", fixedLength: true, maxLength: 5, nullable: false),
                    settlement = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    settlement_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    borough = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    state_code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    borough_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    settlement_id = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    area_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postal_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_postal_codes_code",
                table: "postal_codes",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_postal_codes_state_code_borough_code_settlement_id",
                table: "postal_codes",
                columns: new[] { "state_code", "borough_code", "settlement_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "postal_codes");
        }
    }
}
