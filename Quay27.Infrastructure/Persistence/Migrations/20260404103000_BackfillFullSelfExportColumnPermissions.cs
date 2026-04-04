using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillFullSelfExportColumnPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO ColumnPermissions (Id, UserId, TableName, ColumnName, CanView, CanEdit)
                SELECT UUID(), e.UserId, e.TableName, 'FullSelfExport', e.CanView, e.CanEdit
                FROM ColumnPermissions e
                WHERE e.TableName = 'Customers' AND e.ColumnName = 'Export27'
                AND NOT EXISTS (
                  SELECT 1 FROM ColumnPermissions x
                  WHERE x.UserId = e.UserId AND x.TableName = 'Customers' AND x.ColumnName = 'FullSelfExport'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM ColumnPermissions WHERE TableName = 'Customers' AND ColumnName = 'FullSelfExport';");
        }
    }
}
