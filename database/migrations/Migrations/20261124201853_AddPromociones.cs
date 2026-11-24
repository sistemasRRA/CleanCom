using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPromociones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promociones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    programa = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    linea_servicio_id = table.Column<int>(type: "int", nullable: true),
                    discount_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    value = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    minimum_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    payment_method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    stackable = table.Column<bool>(type: "bit", nullable: false),
                    trigger_linea_servicio_id = table.Column<int>(type: "int", nullable: true),
                    condition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    validity_days = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promociones", x => x.id);
                    table.ForeignKey(
                        name: "fk_promociones_lineas_servicio_linea_servicio_id",
                        column: x => x.linea_servicio_id,
                        principalTable: "lineas_servicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promociones_lineas_servicio_trigger_linea_servicio_id",
                        column: x => x.trigger_linea_servicio_id,
                        principalTable: "lineas_servicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "beneficios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    promocion_id = table.Column<int>(type: "int", nullable: false),
                    invitado_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    origin = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    granted_by_pedido_id = table.Column<int>(type: "int", nullable: true),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: false),
                    pedido_id = table.Column<int>(type: "int", nullable: true),
                    used_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beneficios", x => x.id);
                    table.ForeignKey(
                        name: "fk_beneficios_invitados_invitado_id",
                        column: x => x.invitado_id,
                        principalTable: "invitados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_beneficios_pedidos_granted_by_pedido_id",
                        column: x => x.granted_by_pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_beneficios_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_beneficios_promociones_promocion_id",
                        column: x => x.promocion_id,
                        principalTable: "promociones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "created_at", "created_by", "effective_from", "key", "modified_at", "modified_by", "note", "type", "value" },
                values: new object[,]
                {
                    { 43, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Promociones.MetaCanjeSellos", null, null, "Valor inicial", "Integer", "40" },
                    { 44, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Promociones.MetaCanjeCuponera", null, null, "Valor inicial", "Integer", "30" },
                    { 45, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Promociones.TopeSello", null, null, "Valor inicial", "Integer", "30" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_beneficios_code",
                table: "beneficios",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_beneficios_granted_by_pedido_id",
                table: "beneficios",
                column: "granted_by_pedido_id");

            migrationBuilder.CreateIndex(
                name: "ix_beneficios_invitado_id_promocion_id",
                table: "beneficios",
                columns: new[] { "invitado_id", "promocion_id" });

            migrationBuilder.CreateIndex(
                name: "ix_beneficios_pedido_id",
                table: "beneficios",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "ix_beneficios_promocion_id",
                table: "beneficios",
                column: "promocion_id");

            migrationBuilder.CreateIndex(
                name: "ix_promociones_linea_servicio_id",
                table: "promociones",
                column: "linea_servicio_id");

            migrationBuilder.CreateIndex(
                name: "ix_promociones_programa_name",
                table: "promociones",
                columns: new[] { "programa", "name" },
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_promociones_trigger_linea_servicio_id",
                table: "promociones",
                column: "trigger_linea_servicio_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beneficios");

            migrationBuilder.DropTable(
                name: "promociones");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 45);
        }
    }
}
