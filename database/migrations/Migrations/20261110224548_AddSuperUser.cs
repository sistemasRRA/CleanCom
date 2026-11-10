using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "concurrency_stamp", "display_name", "name", "normalized_name" },
                values: new object[] { 8, "role-superuser", "SuperUser", "SuperUser", "SUPERUSER" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 8);
        }
    }
}
