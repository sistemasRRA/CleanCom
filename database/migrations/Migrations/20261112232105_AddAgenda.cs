using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invitados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    whats_app_phone = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: false),
                    other_phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    how_found_us = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    campaign = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitados", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitados_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ruta_dias",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ruta_id = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    operador_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ruta_dias", x => x.id);
                    table.ForeignKey(
                        name: "fk_ruta_dias_operadores_operador_id",
                        column: x => x.operador_id,
                        principalTable: "operadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ruta_dias_rutas_ruta_id",
                        column: x => x.ruta_id,
                        principalTable: "rutas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "direcciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invitado_id = table.Column<int>(type: "int", nullable: false),
                    label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    exterior_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    interior_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    postal_code = table.Column<string>(type: "nchar(5)", fixedLength: true, maxLength: 5, nullable: false),
                    neighborhood = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    borough = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    references = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    location = table.Column<Point>(type: "geography", nullable: true),
                    zona_id = table.Column<int>(type: "int", nullable: true),
                    zona_assigned_manually = table.Column<bool>(type: "bit", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_direcciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_direcciones_invitados_invitado_id",
                        column: x => x.invitado_id,
                        principalTable: "invitados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_direcciones_zonas_zona_id",
                        column: x => x.zona_id,
                        principalTable: "zonas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedidos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invitado_id = table.Column<int>(type: "int", nullable: false),
                    direccion_id = table.Column<int>(type: "int", nullable: false),
                    etapa = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    garments_approx = table.Column<int>(type: "int", nullable: true),
                    turno = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    specific_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    priority = table.Column<bool>(type: "bit", nullable: false),
                    driver_notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    registered_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    origin = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    recoleccion_date = table.Column<DateOnly>(type: "date", nullable: false),
                    entrega_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos", x => x.id);
                    table.ForeignKey(
                        name: "fk_pedidos_direcciones_direccion_id",
                        column: x => x.direccion_id,
                        principalTable: "direcciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_invitados_invitado_id",
                        column: x => x.invitado_id,
                        principalTable: "invitados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paradas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ruta_dia_id = table.Column<int>(type: "int", nullable: false),
                    kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    pedido_id = table.Column<int>(type: "int", nullable: true),
                    direccion_id = table.Column<int>(type: "int", nullable: true),
                    lugar_id = table.Column<int>(type: "int", nullable: true),
                    turno = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    position = table.Column<int>(type: "int", nullable: false),
                    state = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paradas", x => x.id);
                    table.ForeignKey(
                        name: "fk_paradas_direcciones_direccion_id",
                        column: x => x.direccion_id,
                        principalTable: "direcciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paradas_lugares_lugar_id",
                        column: x => x.lugar_id,
                        principalTable: "lugares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paradas_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paradas_ruta_dias_ruta_dia_id",
                        column: x => x.ruta_dia_id,
                        principalTable: "ruta_dias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedido_lineas_servicio",
                columns: table => new
                {
                    pedido_id = table.Column<int>(type: "int", nullable: false),
                    linea_servicio_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedido_lineas_servicio", x => new { x.pedido_id, x.linea_servicio_id });
                    table.ForeignKey(
                        name: "fk_pedido_lineas_servicio_lineas_servicio_linea_servicio_id",
                        column: x => x.linea_servicio_id,
                        principalTable: "lineas_servicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedido_lineas_servicio_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_direcciones_invitado_id",
                table: "direcciones",
                column: "invitado_id");

            migrationBuilder.CreateIndex(
                name: "ix_direcciones_zona_id",
                table: "direcciones",
                column: "zona_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitados_name",
                table: "invitados",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_invitados_user_id",
                table: "invitados",
                column: "user_id",
                unique: true,
                filter: "[user_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_invitados_whats_app_phone",
                table: "invitados",
                column: "whats_app_phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paradas_direccion_id",
                table: "paradas",
                column: "direccion_id");

            migrationBuilder.CreateIndex(
                name: "ix_paradas_lugar_id",
                table: "paradas",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "ix_paradas_pedido_id",
                table: "paradas",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "ix_paradas_ruta_dia_id_kind",
                table: "paradas",
                columns: new[] { "ruta_dia_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_pedido_lineas_servicio_linea_servicio_id",
                table: "pedido_lineas_servicio",
                column: "linea_servicio_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_direccion_id",
                table: "pedidos",
                column: "direccion_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_etapa_recoleccion_date",
                table: "pedidos",
                columns: new[] { "etapa", "recoleccion_date" });

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_invitado_id",
                table: "pedidos",
                column: "invitado_id");

            migrationBuilder.CreateIndex(
                name: "ix_ruta_dias_operador_id",
                table: "ruta_dias",
                column: "operador_id");

            migrationBuilder.CreateIndex(
                name: "ix_ruta_dias_ruta_id_date",
                table: "ruta_dias",
                columns: new[] { "ruta_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paradas");

            migrationBuilder.DropTable(
                name: "pedido_lineas_servicio");

            migrationBuilder.DropTable(
                name: "ruta_dias");

            migrationBuilder.DropTable(
                name: "pedidos");

            migrationBuilder.DropTable(
                name: "direcciones");

            migrationBuilder.DropTable(
                name: "invitados");
        }
    }
}
