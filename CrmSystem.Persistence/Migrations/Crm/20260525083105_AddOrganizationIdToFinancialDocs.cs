using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSystem.Persistence.Migrations.Crm
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToFinancialDocs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "upds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                -- Remove payment_allocations referencing orphaned invoices or payments
                DELETE FROM payment_allocations WHERE "InvoiceId" IN (
                    SELECT "Id" FROM invoices WHERE NOT EXISTS (
                        SELECT 1 FROM organizations WHERE "CompanyId" = invoices."CompanyId" AND NOT "IsDeleted"
                    )
                ) OR "PaymentId" IN (
                    SELECT "Id" FROM payments WHERE NOT EXISTS (
                        SELECT 1 FROM organizations WHERE "CompanyId" = payments."CompanyId" AND NOT "IsDeleted"
                    )
                );

                -- Remove child lines for orphaned parent docs
                DELETE FROM order_lines WHERE "OrderId" IN (
                    SELECT "Id" FROM orders WHERE NOT EXISTS (
                        SELECT 1 FROM organizations WHERE "CompanyId" = orders."CompanyId" AND NOT "IsDeleted"
                    )
                );
                DELETE FROM invoice_lines WHERE "InvoiceId" IN (
                    SELECT "Id" FROM invoices WHERE NOT EXISTS (
                        SELECT 1 FROM organizations WHERE "CompanyId" = invoices."CompanyId" AND NOT "IsDeleted"
                    )
                );
                DELETE FROM upd_lines WHERE "UpdId" IN (
                    SELECT "Id" FROM upds WHERE NOT EXISTS (
                        SELECT 1 FROM organizations WHERE "CompanyId" = upds."CompanyId" AND NOT "IsDeleted"
                    )
                );

                -- Remove orphaned parent docs (company has no active organization)
                DELETE FROM orders WHERE NOT EXISTS (
                    SELECT 1 FROM organizations WHERE "CompanyId" = orders."CompanyId" AND NOT "IsDeleted"
                );
                DELETE FROM invoices WHERE NOT EXISTS (
                    SELECT 1 FROM organizations WHERE "CompanyId" = invoices."CompanyId" AND NOT "IsDeleted"
                );
                DELETE FROM upds WHERE NOT EXISTS (
                    SELECT 1 FROM organizations WHERE "CompanyId" = upds."CompanyId" AND NOT "IsDeleted"
                );
                DELETE FROM payments WHERE NOT EXISTS (
                    SELECT 1 FROM organizations WHERE "CompanyId" = payments."CompanyId" AND NOT "IsDeleted"
                );

                -- Backfill OrganizationId from the company's default (or first) active organization
                UPDATE orders o
                SET "OrganizationId" = (
                    SELECT org."Id"
                    FROM organizations org
                    WHERE org."CompanyId" = o."CompanyId"
                      AND NOT org."IsDeleted"
                    ORDER BY org."IsDefault" DESC, org."CreatedAt", org."Id"
                    LIMIT 1
                )
                WHERE o."OrganizationId" = '00000000-0000-0000-0000-000000000000';

                UPDATE invoices i
                SET "OrganizationId" = (
                    SELECT org."Id"
                    FROM organizations org
                    WHERE org."CompanyId" = i."CompanyId"
                      AND NOT org."IsDeleted"
                    ORDER BY org."IsDefault" DESC, org."CreatedAt", org."Id"
                    LIMIT 1
                )
                WHERE i."OrganizationId" = '00000000-0000-0000-0000-000000000000';

                UPDATE upds u
                SET "OrganizationId" = (
                    SELECT org."Id"
                    FROM organizations org
                    WHERE org."CompanyId" = u."CompanyId"
                      AND NOT org."IsDeleted"
                    ORDER BY org."IsDefault" DESC, org."CreatedAt", org."Id"
                    LIMIT 1
                )
                WHERE u."OrganizationId" = '00000000-0000-0000-0000-000000000000';

                UPDATE payments p
                SET "OrganizationId" = (
                    SELECT org."Id"
                    FROM organizations org
                    WHERE org."CompanyId" = p."CompanyId"
                      AND NOT org."IsDeleted"
                    ORDER BY org."IsDefault" DESC, org."CreatedAt", org."Id"
                    LIMIT 1
                )
                WHERE p."OrganizationId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_upds_OrganizationId",
                table: "upds",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_OrganizationId",
                table: "payments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrganizationId",
                table: "orders",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_OrganizationId",
                table: "invoices",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_organizations_OrganizationId",
                table: "invoices",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_organizations_OrganizationId",
                table: "orders",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_organizations_OrganizationId",
                table: "payments",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_upds_organizations_OrganizationId",
                table: "upds",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_organizations_OrganizationId",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_organizations_OrganizationId",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_organizations_OrganizationId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_upds_organizations_OrganizationId",
                table: "upds");

            migrationBuilder.DropIndex(
                name: "IX_upds_OrganizationId",
                table: "upds");

            migrationBuilder.DropIndex(
                name: "IX_payments_OrganizationId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_orders_OrganizationId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_invoices_OrganizationId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "upds");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "invoices");
        }
    }
}
