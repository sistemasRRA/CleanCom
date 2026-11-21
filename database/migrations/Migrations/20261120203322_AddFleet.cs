using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFleet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "unidad_id",
                table: "ruta_dias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fleet_alert_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    @event = table.Column<string>(name: "event", type: "nvarchar(30)", maxLength: 30, nullable: false),
                    days_before = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    recipients = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    channels = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fleet_alert_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unidades",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    plate = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    brand = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    model = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    garment_capacity = table.Column<int>(type: "int", nullable: true),
                    km = table.Column<int>(type: "int", nullable: false),
                    km_updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    state = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    service_every_km = table.Column<int>(type: "int", nullable: true),
                    service_every_months = table.Column<int>(type: "int", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unidades", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recurring_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    unidad_id = table.Column<int>(type: "int", nullable: false),
                    concept = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    frequency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    next_date = table.Column<DateOnly>(type: "date", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recurring_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_recurring_payments_unidades_unidad_id",
                        column: x => x.unidad_id,
                        principalTable: "unidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unidad_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    unidad_id = table.Column<int>(type: "int", nullable: false),
                    kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    km = table.Column<int>(type: "int", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    resolved_on = table.Column<DateOnly>(type: "date", nullable: true),
                    recurring_payment_id = table.Column<int>(type: "int", nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unidad_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_unidad_events_recurring_payments_recurring_payment_id",
                        column: x => x.recurring_payment_id,
                        principalTable: "recurring_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unidad_events_unidades_unidad_id",
                        column: x => x.unidad_id,
                        principalTable: "unidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unidad_documents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    unidad_id = table.Column<int>(type: "int", nullable: false),
                    kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    event_id = table.Column<int>(type: "int", nullable: true),
                    file_path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unidad_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_unidad_documents_unidad_events_event_id",
                        column: x => x.event_id,
                        principalTable: "unidad_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unidad_documents_unidades_unidad_id",
                        column: x => x.unidad_id,
                        principalTable: "unidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "fleet_alert_rules",
                columns: new[] { "id", "channels", "created_at", "created_by", "days_before", "event", "modified_at", "modified_by", "recipients" },
                values: new object[,]
                {
                    { 1, "Correo,Web", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "15,0", "Verificacion", null, null, "Administracion" },
                    { 2, "Correo,Web", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "30,7,0", "Seguro", null, null, "Administracion" },
                    { 3, "Correo,Web", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "15,3", "PagoRecurrente", null, null, "Administracion" },
                    { 4, "Correo,Web", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "30", "TarjetaCirculacion", null, null, "Administracion" },
                    { 5, "Correo,Web", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "15,0", "Servicio", null, null, "Administracion" },
                    { 6, "Correo,Web", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "0", "Falla", null, null, "Administracion" }
                });

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "created_at", "created_by", "effective_from", "key", "modified_at", "modified_by", "note", "type", "value" },
                values: new object[,]
                {
                    { 35, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Unidad.ServicioCadaKm", null, null, "Valor inicial", "Integer", "10000" },
                    { 36, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Unidad.ServicioCadaMeses", null, null, "Valor inicial", "Integer", "6" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_ruta_dias_unidad_id",
                table: "ruta_dias",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_fleet_alert_rules_event",
                table: "fleet_alert_rules",
                column: "event",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recurring_payments_unidad_id",
                table: "recurring_payments",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_unidad_documents_event_id",
                table: "unidad_documents",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_unidad_documents_unidad_id_kind",
                table: "unidad_documents",
                columns: new[] { "unidad_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_unidad_events_recurring_payment_id",
                table: "unidad_events",
                column: "recurring_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_unidad_events_unidad_id_date",
                table: "unidad_events",
                columns: new[] { "unidad_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_unidades_plate",
                table: "unidades",
                column: "plate",
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.AddForeignKey(
                name: "fk_ruta_dias_unidades_unidad_id",
                table: "ruta_dias",
                column: "unidad_id",
                principalTable: "unidades",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ruta_dias_unidades_unidad_id",
                table: "ruta_dias");

            migrationBuilder.DropTable(
                name: "fleet_alert_rules");

            migrationBuilder.DropTable(
                name: "unidad_documents");

            migrationBuilder.DropTable(
                name: "unidad_events");

            migrationBuilder.DropTable(
                name: "recurring_payments");

            migrationBuilder.DropTable(
                name: "unidades");

            migrationBuilder.DropIndex(
                name: "ix_ruta_dias_unidad_id",
                table: "ruta_dias");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "id",
                keyValue: 36);

            migrationBuilder.DropColumn(
                name: "unidad_id",
                table: "ruta_dias");
        }
    }
}
