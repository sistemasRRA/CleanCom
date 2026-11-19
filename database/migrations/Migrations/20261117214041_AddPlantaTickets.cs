using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantaTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    folio = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    pedido_id = table.Column<int>(type: "int", nullable: false),
                    linea_servicio_id = table.Column<int>(type: "int", nullable: false),
                    garments = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    state = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ready_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets", x => x.id);
                    table.ForeignKey(
                        name: "fk_tickets_lineas_servicio_linea_servicio_id",
                        column: x => x.linea_servicio_id,
                        principalTable: "lineas_servicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tickets_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tickets_folio",
                table: "tickets",
                column: "folio",
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_linea_servicio_id",
                table: "tickets",
                column: "linea_servicio_id");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_pedido_id_state",
                table: "tickets",
                columns: new[] { "pedido_id", "state" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tickets");
        }
    }
}
