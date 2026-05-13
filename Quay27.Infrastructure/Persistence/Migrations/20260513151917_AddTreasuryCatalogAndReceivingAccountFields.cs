using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTreasuryCatalogAndReceivingAccountFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
                name: "AccountKind",
                table: "ReceivingAccounts",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Bank")
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ReceivingAccounts",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

        migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ReceivingAccounts",
                type: "longtext",
                nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
                name: "ProviderCode",
                table: "ReceivingAccounts",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
                name: "ScopeKind",
                table: "ReceivingAccounts",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "SystemWide")
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
                name: "BankCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullName = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GlobalName = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderNum = table.Column<int>(type: "int", nullable: false),
                    SearchText = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_BankCatalogItems", x => x.Id); })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
                name: "EWalletCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GlobalName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderNum = table.Column<int>(type: "int", nullable: false),
                    SearchText = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_EWalletCatalogItems", x => x.Id); })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_ReceivingAccounts_AccountKind",
            table: "ReceivingAccounts",
            column: "AccountKind");

        migrationBuilder.CreateIndex(
            name: "IX_BankCatalogItems_Code",
            table: "BankCatalogItems",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EWalletCatalogItems_Code",
            table: "EWalletCatalogItems",
            column: "Code",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BankCatalogItems");

        migrationBuilder.DropTable(
            name: "EWalletCatalogItems");

        migrationBuilder.DropIndex(
            name: "IX_ReceivingAccounts_AccountKind",
            table: "ReceivingAccounts");

        migrationBuilder.DropColumn(
            name: "AccountKind",
            table: "ReceivingAccounts");

        migrationBuilder.DropColumn(
            name: "BranchId",
            table: "ReceivingAccounts");

        migrationBuilder.DropColumn(
            name: "Note",
            table: "ReceivingAccounts");

        migrationBuilder.DropColumn(
            name: "ProviderCode",
            table: "ReceivingAccounts");

        migrationBuilder.DropColumn(
            name: "ScopeKind",
            table: "ReceivingAccounts");
    }
}
