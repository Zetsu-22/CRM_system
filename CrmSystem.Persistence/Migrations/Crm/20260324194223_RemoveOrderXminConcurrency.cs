using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSystem.Persistence.Migrations.Crm
{
    /// <inheritdoc />
    public partial class RemoveOrderXminConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin is a PostgreSQL system column — it cannot be renamed or altered.
            // We simply stop tracking it as a concurrency token in the EF model.
            // No DDL changes required; the snapshot is updated to reflect the new model state.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No DDL to reverse.
        }
    }
}
