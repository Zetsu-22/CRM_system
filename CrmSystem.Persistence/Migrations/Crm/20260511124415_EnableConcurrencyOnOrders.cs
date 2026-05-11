using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSystem.Persistence.Migrations.Crm
{
    /// <inheritdoc />
    public partial class EnableConcurrencyOnOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: xmin — системный столбец PostgreSQL, физически уже существует.
            // Эта миграция фиксирует только модельное изменение: SalesOrder.RowVersion
            // снова используется как concurrency token (xmin), вместо b.Ignore.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op (см. Up).
        }
    }
}
