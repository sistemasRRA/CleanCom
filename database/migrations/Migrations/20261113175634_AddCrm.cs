using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCrm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "do_not_contact",
                table: "invitados",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "fixed_frequency_days",
                table: "invitados",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manual_status",
                table: "invitados",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "next_contact_on",
                table: "invitados",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contactos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invitado_id = table.Column<int>(type: "int", nullable: false),
                    channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    next_contact_on = table.Column<DateOnly>(type: "date", nullable: true),
                    contacted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contactos", x => x.id);
                    table.ForeignKey(
                        name: "fk_contactos_invitados_invitado_id",
                        column: x => x.invitado_id,
                        principalTable: "invitados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "created_at", "created_by", "effective_from", "key", "modified_at", "modified_by", "note", "type", "value" },
                values: new object[,]
                {
                    { 27, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Crm.DiasNoContesto", null, null, "Valor inicial", "Integer", "1" },
                    { 28, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Crm.DiasLlamarDespues", null, null, "Valor inicial", "Integer", "3" },
                    { 29, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Crm.DiasNoPorAhora", null, null, "Valor inicial", "Integer", "21" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_contactos_invitado_id_contacted_at",
                table: "contactos",
                columns: new[] { "invitado_id", "contacted_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contactos");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 29);

            migrationBuilder.DropColumn(
                name: "do_not_contact",
                table: "invitados");

            migrationBuilder.DropColumn(
                name: "fixed_frequency_days",
                table: "invitados");

            migrationBuilder.DropColumn(
                name: "manual_status",
                table: "invitados");

            migrationBuilder.DropColumn(
                name: "next_contact_on",
                table: "invitados");
        }
    }
}
