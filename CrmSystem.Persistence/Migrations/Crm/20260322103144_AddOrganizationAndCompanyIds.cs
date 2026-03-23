using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSystem.Persistence.Migrations.Crm
{
    /// <inheritdoc />
    public partial class AddOrganizationAndCompanyIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_items_Sku",
                table: "items");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "warehouses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "counterparties",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organizations_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_CompanyId",
                table: "warehouses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_items_CompanyId",
                table: "items",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_items_CompanyId_Sku",
                table: "items",
                columns: new[] { "CompanyId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_counterparties_CompanyId",
                table: "counterparties",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_CompanyId",
                table: "organizations",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_CompanyId",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_items_CompanyId",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_CompanyId_Sku",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_counterparties_CompanyId",
                table: "counterparties");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "items");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "counterparties");

            migrationBuilder.CreateIndex(
                name: "IX_items_Sku",
                table: "items",
                column: "Sku",
                unique: true);
        }
    }
}
