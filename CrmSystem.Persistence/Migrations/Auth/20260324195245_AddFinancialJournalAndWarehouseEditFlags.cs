using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSystem.Persistence.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AddFinancialJournalAndWarehouseEditFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAccessFinancialJournalEdit",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAccessWarehousesEdit",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccessFinancialJournalEdit",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsAccessWarehousesEdit",
                table: "users");
        }
    }
}
