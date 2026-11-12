using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddBajaLogica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_postal_code_coverage_postal_code_neighborhood",
                table: "postal_code_coverage");

            migrationBuilder.DropIndex(
                name: "ix_non_working_days_date",
                table: "non_working_days");

            migrationBuilder.DropIndex(
                name: "ix_invitados_whats_app_phone",
                table: "invitados");

            migrationBuilder.AddColumn<bool>(
                name: "active",
                table: "postal_code_coverage",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "active",
                table: "pedidos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "active",
                table: "paradas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "active",
                table: "non_working_days",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_postal_code_coverage_postal_code_neighborhood",
                table: "postal_code_coverage",
                columns: new[] { "postal_code", "neighborhood" },
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_non_working_days_date",
                table: "non_working_days",
                column: "date",
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_invitados_whats_app_phone",
                table: "invitados",
                column: "whats_app_phone",
                unique: true,
                filter: "[active] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_postal_code_coverage_postal_code_neighborhood",
                table: "postal_code_coverage");

            migrationBuilder.DropIndex(
                name: "ix_non_working_days_date",
                table: "non_working_days");

            migrationBuilder.DropIndex(
                name: "ix_invitados_whats_app_phone",
                table: "invitados");

            migrationBuilder.DropColumn(
                name: "active",
                table: "postal_code_coverage");

            migrationBuilder.DropColumn(
                name: "active",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "active",
                table: "paradas");

            migrationBuilder.DropColumn(
                name: "active",
                table: "non_working_days");

            migrationBuilder.CreateIndex(
                name: "ix_postal_code_coverage_postal_code_neighborhood",
                table: "postal_code_coverage",
                columns: new[] { "postal_code", "neighborhood" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_non_working_days_date",
                table: "non_working_days",
                column: "date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitados_whats_app_phone",
                table: "invitados",
                column: "whats_app_phone",
                unique: true);
        }
    }
}
