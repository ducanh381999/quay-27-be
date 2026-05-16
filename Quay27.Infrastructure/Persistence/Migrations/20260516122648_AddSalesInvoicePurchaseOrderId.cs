using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoicePurchaseOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "SalesInvoices",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_PurchaseOrderId",
                table: "SalesInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_PurchaseOrders_PurchaseOrderId",
                table: "SalesInvoices",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_PurchaseOrders_PurchaseOrderId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_PurchaseOrderId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "SalesInvoices");
        }
    }
}
