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
        migrationBuilder.AddColumn<Guid>(
            name: "ReceivingAccountId",
            table: "SalesInvoices",
            type: "char(36)",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PriceListId",
            table: "SalesReturns",
            type: "char(36)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PaymentMethod",
            table: "SalesReturns",
            type: "varchar(32)",
            maxLength: 32,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<Guid>(
            name: "ReceivingAccountId",
            table: "SalesReturns",
            type: "char(36)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PaidAmount",
            table: "SalesReturns",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "RefundPaymentMethod",
            table: "SalesReturns",
            type: "varchar(32)",
            maxLength: 32,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<Guid>(
            name: "RefundReceivingAccountId",
            table: "SalesReturns",
            type: "char(36)",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_SalesInvoices_ReceivingAccountId",
            table: "SalesInvoices",
            column: "ReceivingAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_SalesReturns_PriceListId",
            table: "SalesReturns",
            column: "PriceListId");

        migrationBuilder.CreateIndex(
            name: "IX_SalesReturns_ReceivingAccountId",
            table: "SalesReturns",
            column: "ReceivingAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_SalesReturns_RefundReceivingAccountId",
            table: "SalesReturns",
            column: "RefundReceivingAccountId");

        migrationBuilder.AddForeignKey(
            name: "FK_SalesInvoices_ReceivingAccounts_ReceivingAccountId",
            table: "SalesInvoices",
            column: "ReceivingAccountId",
            principalTable: "ReceivingAccounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_SalesReturns_PriceLists_PriceListId",
            table: "SalesReturns",
            column: "PriceListId",
            principalTable: "PriceLists",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_SalesReturns_ReceivingAccounts_ReceivingAccountId",
            table: "SalesReturns",
            column: "ReceivingAccountId",
            principalTable: "ReceivingAccounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_SalesReturns_ReceivingAccounts_RefundReceivingAccountId",
            table: "SalesReturns",
            column: "RefundReceivingAccountId",
            principalTable: "ReceivingAccounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_SalesInvoices_ReceivingAccounts_ReceivingAccountId",
            table: "SalesInvoices");

        migrationBuilder.DropForeignKey(
            name: "FK_SalesReturns_PriceLists_PriceListId",
            table: "SalesReturns");

        migrationBuilder.DropForeignKey(
            name: "FK_SalesReturns_ReceivingAccounts_ReceivingAccountId",
            table: "SalesReturns");

        migrationBuilder.DropForeignKey(
            name: "FK_SalesReturns_ReceivingAccounts_RefundReceivingAccountId",
            table: "SalesReturns");

        migrationBuilder.DropIndex(
            name: "IX_SalesInvoices_ReceivingAccountId",
            table: "SalesInvoices");

        migrationBuilder.DropIndex(
            name: "IX_SalesReturns_PriceListId",
            table: "SalesReturns");

        migrationBuilder.DropIndex(
            name: "IX_SalesReturns_ReceivingAccountId",
            table: "SalesReturns");

        migrationBuilder.DropIndex(
            name: "IX_SalesReturns_RefundReceivingAccountId",
            table: "SalesReturns");

        migrationBuilder.DropColumn(name: "ReceivingAccountId", table: "SalesInvoices");
        migrationBuilder.DropColumn(name: "PriceListId", table: "SalesReturns");
        migrationBuilder.DropColumn(name: "PaymentMethod", table: "SalesReturns");
        migrationBuilder.DropColumn(name: "ReceivingAccountId", table: "SalesReturns");
        migrationBuilder.DropColumn(name: "PaidAmount", table: "SalesReturns");
        migrationBuilder.DropColumn(name: "RefundPaymentMethod", table: "SalesReturns");
        migrationBuilder.DropColumn(name: "RefundReceivingAccountId", table: "SalesReturns");
    }
}
