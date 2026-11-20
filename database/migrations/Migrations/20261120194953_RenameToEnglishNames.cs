using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <summary>
    /// Direccion, Evidencia, CobroReferencia and Contacto are now Address, Evidence, ReferencePayment and Contact.
    /// Edited by hand: EF Core does not detect a renamed entity and scaffolded DropTable + CreateTable, which loses the
    /// data. This version renames the tables, keys and indexes and keeps every row. The model snapshot is the one EF
    /// generated.
    /// </summary>
    public partial class RenameToEnglishNames : Migration
    {
        private static readonly (string Old, string New)[] Tables =
        [
            ("direcciones", "addresses"),
            ("contactos", "contacts"),
            ("evidencias", "evidence"),
            ("cobros_referencia", "reference_payments"),
        ];

        private static readonly (string OldTable, string NewTable, string Old, string New)[] Indexes =
        [
            ("direcciones", "addresses", "ix_direcciones_invitado_id", "ix_addresses_invitado_id"),
            ("direcciones", "addresses", "ix_direcciones_zona_id", "ix_addresses_zona_id"),
            ("contactos", "contacts", "ix_contactos_invitado_id_contacted_at", "ix_contacts_invitado_id_contacted_at"),
            ("evidencias", "evidence", "ix_evidencias_local_id", "ix_evidence_local_id"),
            ("evidencias", "evidence", "ix_evidencias_parada_id_kind", "ix_evidence_parada_id_kind"),
            ("cobros_referencia", "reference_payments", "ix_cobros_referencia_parada_id", "ix_reference_payments_parada_id"),
        ];

        /// <summary>Foreign keys from the renamed tables: (old table, new table, column, principal table, old name, new name).</summary>
        private static readonly (string OldTable, string NewTable, string Column, string Principal, string Old, string New)[] OwnForeignKeys =
        [
            ("direcciones", "addresses", "invitado_id", "invitados", "fk_direcciones_invitados_invitado_id", "fk_addresses_invitados_invitado_id"),
            ("direcciones", "addresses", "zona_id", "zonas", "fk_direcciones_zonas_zona_id", "fk_addresses_zonas_zona_id"),
            ("contactos", "contacts", "invitado_id", "invitados", "fk_contactos_invitados_invitado_id", "fk_contacts_invitados_invitado_id"),
            ("evidencias", "evidence", "parada_id", "paradas", "fk_evidencias_paradas_parada_id", "fk_evidence_paradas_parada_id"),
            ("cobros_referencia", "reference_payments", "parada_id", "paradas", "fk_cobros_referencia_paradas_parada_id", "fk_reference_payments_paradas_parada_id"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Foreign keys that point to direcciones, and the ones of the renamed tables.
            migrationBuilder.DropForeignKey(name: "fk_paradas_direcciones_direccion_id", table: "paradas");
            migrationBuilder.DropForeignKey(name: "fk_pedidos_direcciones_direccion_id", table: "pedidos");
            foreach (var fk in OwnForeignKeys)
            {
                migrationBuilder.DropForeignKey(name: fk.Old, table: fk.OldTable);
            }

            foreach (var (oldName, newName) in Tables)
            {
                migrationBuilder.DropPrimaryKey(name: $"pk_{oldName}", table: oldName);
                migrationBuilder.RenameTable(name: oldName, newName: newName);
                migrationBuilder.AddPrimaryKey(name: $"pk_{newName}", table: newName, column: "id");
            }
            foreach (var ix in Indexes)
            {
                migrationBuilder.RenameIndex(name: ix.Old, table: ix.NewTable, newName: ix.New);
            }

            migrationBuilder.RenameColumn(name: "direccion_id", table: "pedidos", newName: "address_id");
            migrationBuilder.RenameIndex(name: "ix_pedidos_direccion_id", table: "pedidos", newName: "ix_pedidos_address_id");
            migrationBuilder.RenameColumn(name: "direccion_id", table: "paradas", newName: "address_id");
            migrationBuilder.RenameColumn(name: "address", table: "paradas", newName: "address_text");
            migrationBuilder.RenameIndex(name: "ix_paradas_direccion_id", table: "paradas", newName: "ix_paradas_address_id");

            foreach (var fk in OwnForeignKeys)
            {
                migrationBuilder.AddForeignKey(name: fk.New, table: fk.NewTable, column: fk.Column,
                    principalTable: fk.Principal, principalColumn: "id", onDelete: ReferentialAction.Restrict);
            }
            migrationBuilder.AddForeignKey(name: "fk_paradas_addresses_address_id", table: "paradas", column: "address_id",
                principalTable: "addresses", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "fk_pedidos_addresses_address_id", table: "pedidos", column: "address_id",
                principalTable: "addresses", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_paradas_addresses_address_id", table: "paradas");
            migrationBuilder.DropForeignKey(name: "fk_pedidos_addresses_address_id", table: "pedidos");
            foreach (var fk in OwnForeignKeys)
            {
                migrationBuilder.DropForeignKey(name: fk.New, table: fk.NewTable);
            }

            foreach (var (oldName, newName) in Tables)
            {
                migrationBuilder.DropPrimaryKey(name: $"pk_{newName}", table: newName);
                migrationBuilder.RenameTable(name: newName, newName: oldName);
                migrationBuilder.AddPrimaryKey(name: $"pk_{oldName}", table: oldName, column: "id");
            }
            foreach (var ix in Indexes)
            {
                migrationBuilder.RenameIndex(name: ix.New, table: ix.OldTable, newName: ix.Old);
            }

            migrationBuilder.RenameColumn(name: "address_id", table: "pedidos", newName: "direccion_id");
            migrationBuilder.RenameIndex(name: "ix_pedidos_address_id", table: "pedidos", newName: "ix_pedidos_direccion_id");
            migrationBuilder.RenameColumn(name: "address_id", table: "paradas", newName: "direccion_id");
            migrationBuilder.RenameColumn(name: "address_text", table: "paradas", newName: "address");
            migrationBuilder.RenameIndex(name: "ix_paradas_address_id", table: "paradas", newName: "ix_paradas_direccion_id");

            foreach (var fk in OwnForeignKeys)
            {
                migrationBuilder.AddForeignKey(name: fk.Old, table: fk.OldTable, column: fk.Column,
                    principalTable: fk.Principal, principalColumn: "id", onDelete: ReferentialAction.Restrict);
            }
            migrationBuilder.AddForeignKey(name: "fk_paradas_direcciones_direccion_id", table: "paradas", column: "direccion_id",
                principalTable: "direcciones", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "fk_pedidos_direcciones_direccion_id", table: "pedidos", column: "direccion_id",
                principalTable: "direcciones", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        }
    }
}
