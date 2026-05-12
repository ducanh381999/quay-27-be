using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quay27.Infrastructure.Persistence;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Repairs rows where reward columns are NULL (e.g. manual SQL or partial imports) so EF materialization
    /// of non-nullable CustomerProfile decimals cannot throw.
    /// Optional pre-checks: SELECT COUNT(*) FROM CustomerProfiles WHERE RewardPointsBalance IS NULL;
    /// SELECT COUNT(*) FROM CustomerProfiles WHERE RewardPointsLifetime IS NULL;
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512183000_BackfillNullCustomerProfileRewardPoints")]
    public class BackfillNullCustomerProfileRewardPoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE `CustomerProfiles` SET `RewardPointsBalance` = 0 WHERE `RewardPointsBalance` IS NULL;");
            migrationBuilder.Sql(
                "UPDATE `CustomerProfiles` SET `RewardPointsLifetime` = 0 WHERE `RewardPointsLifetime` IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
