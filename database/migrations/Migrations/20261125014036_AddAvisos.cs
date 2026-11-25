using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAvisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avisos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invitado_id = table.Column<int>(type: "int", nullable: false),
                    pedido_id = table.Column<int>(type: "int", nullable: true),
                    evento = table.Column<int>(type: "int", nullable: false),
                    canal = table.Column<int>(type: "int", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    meta_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    values = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    title = table.Column<string>(type: "nvarchar(65)", maxLength: 65, nullable: true),
                    text = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    state = table.Column<int>(type: "int", nullable: false),
                    detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avisos", x => x.id);
                    table.ForeignKey(
                        name: "fk_avisos_invitados_invitado_id",
                        column: x => x.invitado_id,
                        principalTable: "invitados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_avisos_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "avisos_oficina",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    kind = table.Column<int>(type: "int", nullable: false),
                    invitado_id = table.Column<int>(type: "int", nullable: false),
                    pedido_id = table.Column<int>(type: "int", nullable: true),
                    text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    attended_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    attended_by = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avisos_oficina", x => x.id);
                    table.ForeignKey(
                        name: "fk_avisos_oficina_invitados_invitado_id",
                        column: x => x.invitado_id,
                        principalTable: "invitados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_avisos_oficina_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dispositivos_push",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invitado_id = table.Column<int>(type: "int", nullable: false),
                    token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispositivos_push", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispositivos_push_invitados_invitado_id",
                        column: x => x.invitado_id,
                        principalTable: "invitados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plantillas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    evento = table.Column<int>(type: "int", nullable: false),
                    meta_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    whats_app_text = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    push_title = table.Column<string>(type: "nvarchar(65)", maxLength: 65, nullable: false),
                    push_text = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plantillas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avisos_invitado_id",
                table: "avisos",
                column: "invitado_id");

            migrationBuilder.CreateIndex(
                name: "ix_avisos_pedido_id_evento",
                table: "avisos",
                columns: new[] { "pedido_id", "evento" });

            migrationBuilder.CreateIndex(
                name: "ix_avisos_state",
                table: "avisos",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_avisos_oficina_attended_at",
                table: "avisos_oficina",
                column: "attended_at");

            migrationBuilder.CreateIndex(
                name: "ix_avisos_oficina_invitado_id",
                table: "avisos_oficina",
                column: "invitado_id");

            migrationBuilder.CreateIndex(
                name: "ix_avisos_oficina_pedido_id",
                table: "avisos_oficina",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispositivos_push_invitado_id",
                table: "dispositivos_push",
                column: "invitado_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispositivos_push_token",
                table: "dispositivos_push",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plantillas_evento",
                table: "plantillas",
                column: "evento",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avisos");

            migrationBuilder.DropTable(
                name: "avisos_oficina");

            migrationBuilder.DropTable(
                name: "dispositivos_push");

            migrationBuilder.DropTable(
                name: "plantillas");
        }
    }
}
