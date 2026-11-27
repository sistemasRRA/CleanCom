using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "change_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    entity_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<int>(type: "int", nullable: false),
                    owner_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    owner_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<int>(type: "int", nullable: false),
                    changes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_change_logs_created_at",
                table: "change_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_change_logs_entity_type_entity_id_created_at",
                table: "change_logs",
                columns: new[] { "entity_type", "entity_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_change_logs_owner_type_owner_id_created_at",
                table: "change_logs",
                columns: new[] { "owner_type", "owner_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change_logs");
        }
    }
}
