using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "roles",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1,
                column: "display_name",
                value: "Administración");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2,
                column: "display_name",
                value: "Chofer");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3,
                column: "display_name",
                value: "Invitado");

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "concurrency_stamp", "display_name", "name", "normalized_name" },
                values: new object[,]
                {
                    { 4, "role-gerencia", "Gerencia", "Gerencia", "GERENCIA" },
                    { 5, "role-logistica", "Logística", "Logistica", "LOGISTICA" },
                    { 6, "role-telemarketing", "Telemarketing", "Telemarketing", "TELEMARKETING" },
                    { 7, "role-planta", "Planta", "Planta", "PLANTA" }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "id", "created_at", "created_by", "level", "modified_at", "modified_by", "role_id", "screen" },
                values: new object[,]
                {
                    { 19, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "panel-del-dia" },
                    { 20, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "rutas" },
                    { 21, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "agendar" },
                    { 22, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "pedidos" },
                    { 23, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "clientes-crm" },
                    { 24, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "paradas-fijas" },
                    { 25, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "promociones" },
                    { 26, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "tickets-en-planta" },
                    { 27, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "kpis" },
                    { 28, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "clientes-y-fuentes" },
                    { 29, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "evidencias-y-caja" },
                    { 30, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "vista-del-chofer" },
                    { 31, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "unidades" },
                    { 32, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 4, "whatsapp-y-metas" },
                    { 33, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 5, "panel-del-dia" },
                    { 34, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 5, "rutas" },
                    { 35, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 5, "paradas-fijas" },
                    { 36, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 5, "pedidos" },
                    { 37, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 5, "vista-del-chofer" },
                    { 38, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 5, "unidades" },
                    { 39, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 5, "agendar" },
                    { 40, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 6, "agendar" },
                    { 41, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 6, "clientes-crm" },
                    { 42, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 6, "whatsapp-y-metas" },
                    { 43, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 6, "promociones" },
                    { 44, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 6, "pedidos" },
                    { 45, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit", null, null, 7, "tickets-en-planta" },
                    { 46, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View", null, null, 7, "pedidos" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "roles");
        }
    }
}
