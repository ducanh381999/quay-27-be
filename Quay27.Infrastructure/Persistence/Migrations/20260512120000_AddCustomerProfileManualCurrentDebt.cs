using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quay27.Infrastructure.Persistence;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512120000_AddCustomerProfileManualCurrentDebt")]
    /// <inheritdoc />
    public class AddCustomerProfileManualCurrentDebt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ManualCurrentDebt",
                table: "CustomerProfiles",
                type: "decimal(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManualCurrentDebt",
                table: "CustomerProfiles");
        }
    }
}
