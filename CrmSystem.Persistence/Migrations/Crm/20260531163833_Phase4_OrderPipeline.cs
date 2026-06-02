using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSystem.Persistence.Migrations.Crm
{
    /// <inheritdoc />
    public partial class Phase4_OrderPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankRequisitesSnapshotJson",
                table: "upds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalculationSnapshotJson",
                table: "upds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyInnSnapshot",
                table: "upds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyKppSnapshot",
                table: "upds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressSnapshotJson",
                table: "upds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountSnapshotJson",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressSnapshotJson",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickingChecklistJson",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankRequisitesSnapshotJson",
                table: "invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalculationSnapshotJson",
                table: "invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyInnSnapshot",
                table: "invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyKppSnapshot",
                table: "invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressSnapshotJson",
                table: "invoices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_status_history_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_status_history_OrderId",
                table: "order_status_history",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_status_history");

            migrationBuilder.DropColumn(
                name: "BankRequisitesSnapshotJson",
                table: "upds");

            migrationBuilder.DropColumn(
                name: "CalculationSnapshotJson",
                table: "upds");

            migrationBuilder.DropColumn(
                name: "CounterpartyInnSnapshot",
                table: "upds");

            migrationBuilder.DropColumn(
                name: "CounterpartyKppSnapshot",
                table: "upds");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressSnapshotJson",
                table: "upds");

            migrationBuilder.DropColumn(
                name: "BankAccountSnapshotJson",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressSnapshotJson",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PickingChecklistJson",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "BankRequisitesSnapshotJson",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "CalculationSnapshotJson",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "CounterpartyInnSnapshot",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "CounterpartyKppSnapshot",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressSnapshotJson",
                table: "invoices");
        }
    }
}
