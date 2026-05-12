using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quay27.Infrastructure.Persistence;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512140000_AddCustomerGroupRulesAndProfilePoints")]
    public class AddCustomerGroupRulesAndProfilePoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "CustomerGroups",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DiscountIsPercent",
                table: "CustomerGroups",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RulesJson",
                table: "CustomerGroups",
                type: "longtext",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "CombineAllConditions",
                table: "CustomerGroups",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MembershipUpdateMode",
                table: "CustomerGroups",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoMembershipSync",
                table: "CustomerGroups",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardPointsBalance",
                table: "CustomerProfiles",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardPointsLifetime",
                table: "CustomerProfiles",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RewardPointsLifetime", table: "CustomerProfiles");
            migrationBuilder.DropColumn(name: "RewardPointsBalance", table: "CustomerProfiles");

            migrationBuilder.DropColumn(name: "IsAutoMembershipSync", table: "CustomerGroups");
            migrationBuilder.DropColumn(name: "MembershipUpdateMode", table: "CustomerGroups");
            migrationBuilder.DropColumn(name: "CombineAllConditions", table: "CustomerGroups");
            migrationBuilder.DropColumn(name: "RulesJson", table: "CustomerGroups");
            migrationBuilder.DropColumn(name: "DiscountIsPercent", table: "CustomerGroups");
            migrationBuilder.DropColumn(name: "DiscountAmount", table: "CustomerGroups");
        }
    }
}
