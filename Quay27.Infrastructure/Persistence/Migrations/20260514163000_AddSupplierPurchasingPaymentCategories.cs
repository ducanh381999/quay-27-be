using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quay27.Infrastructure.Persistence;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260514163000_AddSupplierPurchasingPaymentCategories")]
public class AddSupplierPurchasingPaymentCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO `PaymentCategories` (`Code`, `Name`, `Kind`)
            SELECT 'SupplierPurchasePayment', 'Chi Tiền trả NCC', 'Expense'
            WHERE NOT EXISTS (SELECT 1 FROM `PaymentCategories` WHERE `Code` = 'SupplierPurchasePayment' LIMIT 1);
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO `PaymentCategories` (`Code`, `Name`, `Kind`)
            SELECT 'SupplierReturnRefund', 'Thu Tiền NCC hoàn trả', 'Receipt'
            WHERE NOT EXISTS (SELECT 1 FROM `PaymentCategories` WHERE `Code` = 'SupplierReturnRefund' LIMIT 1);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM `PaymentCategories` WHERE `Code` IN ('SupplierPurchasePayment', 'SupplierReturnRefund');
            """);
    }
}
