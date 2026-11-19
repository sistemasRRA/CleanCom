using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPedidoHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "etapa_since",
                table: "pedidos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "failed_attempts",
                table: "pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "pedido_eventos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pedido_id = table.Column<int>(type: "int", nullable: false),
                    kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    from_etapa = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    to_etapa = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    automatic = table.Column<bool>(type: "bit", nullable: false),
                    reason_id = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedido_eventos", x => x.id);
                    table.ForeignKey(
                        name: "fk_pedido_eventos_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedido_eventos_reasons_reason_id",
                        column: x => x.reason_id,
                        principalTable: "reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pedido_eventos_pedido_id_created_at",
                table: "pedido_eventos",
                columns: new[] { "pedido_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pedido_eventos_reason_id",
                table: "pedido_eventos",
                column: "reason_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pedido_eventos");

            migrationBuilder.DropColumn(
                name: "etapa_since",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "failed_attempts",
                table: "pedidos");
        }
    }
}
