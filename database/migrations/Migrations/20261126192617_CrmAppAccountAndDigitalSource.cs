using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class CrmAppAccountAndDigitalSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_digital",
                table: "reasons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "invited_to_app_at",
                table: "invitados",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "created_at", "created_by", "effective_from", "key", "modified_at", "modified_by", "note", "type", "value" },
                values: new object[] { 47, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AppInvitado.MensajeInvitacion", null, null, "Valor inicial", "Text", "Hola {nombre}, ya puedes pedir tu recolección y seguir a tu chofer desde la app de CleanCom. Búscala como CleanCom en App Store o Google Play." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 47);

            migrationBuilder.DropColumn(
                name: "is_digital",
                table: "reasons");

            migrationBuilder.DropColumn(
                name: "invited_to_app_at",
                table: "invitados");
        }
    }
}
