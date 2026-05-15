using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quay27.Infrastructure.Persistence;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260516160000_AddSalesOrderPaymentFields")]
public class AddSalesOrderPaymentFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        MigrationSqlHelper.AddColumnIfNotExists(
            migrationBuilder,
            "SalesInvoices",
            "ReceivingAccountId",
            "char(36) NULL");

        MigrationSqlHelper.AddColumnIfNotExists(
            migrationBuilder,
            "SalesReturns",
            "PriceListId",
            "char(36) NULL");

        MigrationSqlHelper.AddColumnIfNotExists(
            migrationBuilder,
            "SalesReturns",
            "PaymentMethod",
            "varchar(32) NULL");

        MigrationSqlHelper.AddColumnIfNotExists(
            migrationBuilder,
            "SalesReturns",
            "ReceivingAccountId",
            "char(36) NULL");

        MigrationSqlHelper.AddColumnIfNotExists(
            migrationBuilder,
            "SalesReturns",
            "PaidAmount",
            "decimal(18,2) NOT NULL DEFAULT 0");

        MigrationSqlHelper.AddColumnIfNotExists(
            migrationBuilder,
            "SalesReturns",
            "RefundPaymentMethod",
            "varchar(32) NULL");

        MigrationSqlHelper.AddColumnIfNotExists(
            migrationBuilder,
            "SalesReturns",
            "RefundReceivingAccountId",
            "char(36) NULL");

        MigrationSqlHelper.CreateIndexIfNotExists(
            migrationBuilder,
            "IX_SalesInvoices_ReceivingAccountId",
            "SalesInvoices",
            "ReceivingAccountId");

        MigrationSqlHelper.CreateIndexIfNotExists(
            migrationBuilder,
            "IX_SalesReturns_PriceListId",
            "SalesReturns",
            "PriceListId");

        MigrationSqlHelper.CreateIndexIfNotExists(
            migrationBuilder,
            "IX_SalesReturns_ReceivingAccountId",
            "SalesReturns",
            "ReceivingAccountId");

        MigrationSqlHelper.CreateIndexIfNotExists(
            migrationBuilder,
            "IX_SalesReturns_RefundReceivingAccountId",
            "SalesReturns",
            "RefundReceivingAccountId");

        MigrationSqlHelper.AddForeignKeyIfNotExists(
            migrationBuilder,
            "FK_SalesInvoices_ReceivingAccounts_ReceivingAccountId",
            "SalesInvoices",
            "ReceivingAccountId",
            "ReceivingAccounts",
            "Id",
            "SET NULL");

        MigrationSqlHelper.AddForeignKeyIfNotExists(
            migrationBuilder,
            "FK_SalesReturns_PriceLists_PriceListId",
            "SalesReturns",
            "PriceListId",
            "PriceLists",
            "Id",
            "SET NULL");

        MigrationSqlHelper.AddForeignKeyIfNotExists(
            migrationBuilder,
            "FK_SalesReturns_ReceivingAccounts_ReceivingAccountId",
            "SalesReturns",
            "ReceivingAccountId",
            "ReceivingAccounts",
            "Id",
            "SET NULL");

        MigrationSqlHelper.AddForeignKeyIfNotExists(
            migrationBuilder,
            "FK_SalesReturns_ReceivingAccounts_RefundReceivingAccountId",
            "SalesReturns",
            "RefundReceivingAccountId",
            "ReceivingAccounts",
            "Id",
            "SET NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        MigrationSqlHelper.DropForeignKeyIfExists(
            migrationBuilder,
            "FK_SalesInvoices_ReceivingAccounts_ReceivingAccountId",
            "SalesInvoices");

        MigrationSqlHelper.DropForeignKeyIfExists(
            migrationBuilder,
            "FK_SalesReturns_PriceLists_PriceListId",
            "SalesReturns");

        MigrationSqlHelper.DropForeignKeyIfExists(
            migrationBuilder,
            "FK_SalesReturns_ReceivingAccounts_ReceivingAccountId",
            "SalesReturns");

        MigrationSqlHelper.DropForeignKeyIfExists(
            migrationBuilder,
            "FK_SalesReturns_ReceivingAccounts_RefundReceivingAccountId",
            "SalesReturns");

        MigrationSqlHelper.DropIndexIfExists(
            migrationBuilder,
            "IX_SalesInvoices_ReceivingAccountId",
            "SalesInvoices");

        MigrationSqlHelper.DropIndexIfExists(
            migrationBuilder,
            "IX_SalesReturns_PriceListId",
            "SalesReturns");

        MigrationSqlHelper.DropIndexIfExists(
            migrationBuilder,
            "IX_SalesReturns_ReceivingAccountId",
            "SalesReturns");

        MigrationSqlHelper.DropIndexIfExists(
            migrationBuilder,
            "IX_SalesReturns_RefundReceivingAccountId",
            "SalesReturns");

        MigrationSqlHelper.DropColumnIfExists(migrationBuilder, "SalesInvoices", "ReceivingAccountId");
        MigrationSqlHelper.DropColumnIfExists(migrationBuilder, "SalesReturns", "PriceListId");
        MigrationSqlHelper.DropColumnIfExists(migrationBuilder, "SalesReturns", "PaymentMethod");
        MigrationSqlHelper.DropColumnIfExists(migrationBuilder, "SalesReturns", "ReceivingAccountId");
        MigrationSqlHelper.DropColumnIfExists(migrationBuilder, "SalesReturns", "PaidAmount");
        MigrationSqlHelper.DropColumnIfExists(migrationBuilder, "SalesReturns", "RefundPaymentMethod");
        MigrationSqlHelper.DropColumnIfExists(migrationBuilder, "SalesReturns", "RefundReceivingAccountId");
    }
}
