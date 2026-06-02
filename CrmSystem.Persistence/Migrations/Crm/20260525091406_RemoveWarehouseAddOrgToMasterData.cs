using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSystem.Persistence.Migrations.Crm
{
    /// <inheritdoc />
    public partial class RemoveWarehouseAddOrgToMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_lines_warehouses_WarehouseId",
                table: "order_lines");

            migrationBuilder.DropTable(
                name: "stock_balances");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_order_lines_WarehouseId",
                table: "order_lines");

            migrationBuilder.DropIndex(
                name: "IX_items_CompanyId_Sku",
                table: "items");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "upd_lines");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "IsStockItem",
                table: "items");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "counterparties",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                -- Remove orphaned counterparties (company has no active organization)
                DELETE FROM counterparty_contacts WHERE "CounterpartyId" IN (
                    SELECT "Id" FROM counterparties WHERE NOT EXISTS (
                        SELECT 1 FROM organizations WHERE "CompanyId" = counterparties."CompanyId" AND NOT "IsDeleted"
                    )
                );
                DELETE FROM counterparty_addresses WHERE "CounterpartyId" IN (
                    SELECT "Id" FROM counterparties WHERE NOT EXISTS (
                        SELECT 1 FROM organizations WHERE "CompanyId" = counterparties."CompanyId" AND NOT "IsDeleted"
                    )
                );
                DELETE FROM counterparties WHERE NOT EXISTS (
                    SELECT 1 FROM organizations WHERE "CompanyId" = counterparties."CompanyId" AND NOT "IsDeleted"
                );

                -- Remove orphaned items (company has no active organization)
                DELETE FROM items WHERE NOT EXISTS (
                    SELECT 1 FROM organizations WHERE "CompanyId" = items."CompanyId" AND NOT "IsDeleted"
                );

                -- Backfill OrganizationId from company's default (or first) active organization
                UPDATE counterparties SET "OrganizationId" = (
                    SELECT org."Id" FROM organizations org
                    WHERE org."CompanyId" = counterparties."CompanyId" AND NOT org."IsDeleted"
                    ORDER BY org."IsDefault" DESC, org."CreatedAt", org."Id"
                    LIMIT 1
                )
                WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000';

                UPDATE items SET "OrganizationId" = (
                    SELECT org."Id" FROM organizations org
                    WHERE org."CompanyId" = items."CompanyId" AND NOT org."IsDeleted"
                    ORDER BY org."IsDefault" DESC, org."CreatedAt", org."Id"
                    LIMIT 1
                )
                WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_items_OrganizationId",
                table: "items",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_items_OrganizationId_Sku",
                table: "items",
                columns: new[] { "OrganizationId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_counterparties_OrganizationId",
                table: "counterparties",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_counterparties_organizations_OrganizationId",
                table: "counterparties",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_items_organizations_OrganizationId",
                table: "items",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_counterparties_organizations_OrganizationId",
                table: "counterparties");

            migrationBuilder.DropForeignKey(
                name: "FK_items_organizations_OrganizationId",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_OrganizationId",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_OrganizationId_Sku",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_counterparties_OrganizationId",
                table: "counterparties");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "items");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "counterparties");

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "upd_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStockItem",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    QtyDeltaOnHand = table.Column<decimal>(type: "numeric", nullable: false),
                    QtyDeltaReserved = table.Column<decimal>(type: "numeric", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    QtyOnHand = table.Column<decimal>(type: "numeric", nullable: false),
                    QtyReserved = table.Column<decimal>(type: "numeric", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_balances_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_balances_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_WarehouseId",
                table: "order_lines",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_items_CompanyId_Sku",
                table: "items",
                columns: new[] { "CompanyId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_ItemId",
                table: "stock_balances",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_WarehouseId_ItemId",
                table: "stock_balances",
                columns: new[] { "WarehouseId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_Date",
                table: "stock_movements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_SourceType_SourceId",
                table: "stock_movements",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_CompanyId",
                table: "warehouses",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_order_lines_warehouses_WarehouseId",
                table: "order_lines",
                column: "WarehouseId",
                principalTable: "warehouses",
                principalColumn: "Id");
        }
    }
}
