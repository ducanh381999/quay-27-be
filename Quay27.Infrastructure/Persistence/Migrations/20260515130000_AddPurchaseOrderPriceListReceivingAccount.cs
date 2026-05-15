using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quay27.Infrastructure.Persistence;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260515130000_AddPurchaseOrderPriceListReceivingAccount")]
public class AddPurchaseOrderPriceListReceivingAccount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PriceListId",
            table: "PurchaseOrders",
            type: "char(36)",
            nullable: true,
            collation: "ascii_general_ci");

        migrationBuilder.AddColumn<Guid>(
            name: "ReceivingAccountId",
            table: "PurchaseOrders",
            type: "char(36)",
            nullable: true,
            collation: "ascii_general_ci");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrders_PriceListId",
            table: "PurchaseOrders",
            column: "PriceListId");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrders_ReceivingAccountId",
            table: "PurchaseOrders",
            column: "ReceivingAccountId");

        migrationBuilder.AddForeignKey(
            name: "FK_PurchaseOrders_PriceLists_PriceListId",
            table: "PurchaseOrders",
            column: "PriceListId",
            principalTable: "PriceLists",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_PurchaseOrders_ReceivingAccounts_ReceivingAccountId",
            table: "PurchaseOrders",
            column: "ReceivingAccountId",
            principalTable: "ReceivingAccounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PurchaseOrders_PriceLists_PriceListId",
            table: "PurchaseOrders");

        migrationBuilder.DropForeignKey(
            name: "FK_PurchaseOrders_ReceivingAccounts_ReceivingAccountId",
            table: "PurchaseOrders");

        migrationBuilder.DropIndex(
            name: "IX_PurchaseOrders_PriceListId",
            table: "PurchaseOrders");

        migrationBuilder.DropIndex(
            name: "IX_PurchaseOrders_ReceivingAccountId",
            table: "PurchaseOrders");

        migrationBuilder.DropColumn(
            name: "PriceListId",
            table: "PurchaseOrders");

        migrationBuilder.DropColumn(
            name: "ReceivingAccountId",
            table: "PurchaseOrders");
    }
}
