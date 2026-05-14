using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quay27.Infrastructure.Persistence;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260514140000_AddSupplierPayablesModule")]
public class AddSupplierPayablesModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "SupplierPayableDiscountPortion",
            table: "GoodsReceipts",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.CreateTable(
            name: "SupplierDebtAdjustments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                SupplierId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                Delta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Description = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierDebtAdjustments", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierDebtAdjustments_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierDebtAdjustments_Code",
            table: "SupplierDebtAdjustments",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierDebtAdjustments_SupplierId_OccurredAtUtc",
            table: "SupplierDebtAdjustments",
            columns: new[] { "SupplierId", "OccurredAtUtc" });

        migrationBuilder.CreateTable(
            name: "SupplierPayablePayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                SupplierId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                PayerUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                PaymentMethod = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ReceivingAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Note = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                AllocateToDocuments = table.Column<bool>(type: "tinyint(1)", nullable: false),
                PostToCashbook = table.Column<bool>(type: "tinyint(1)", nullable: false),
                CashbookEntryId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierPayablePayments", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierPayablePayments_CashbookEntries_CashbookEntryId",
                    column: x => x.CashbookEntryId,
                    principalTable: "CashbookEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_SupplierPayablePayments_ReceivingAccounts_ReceivingAccountId",
                    column: x => x.ReceivingAccountId,
                    principalTable: "ReceivingAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierPayablePayments_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierPayablePayments_Users_PayerUserId",
                    column: x => x.PayerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierPayablePayments_Code",
            table: "SupplierPayablePayments",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierPayablePayments_SupplierId_OccurredAtUtc",
            table: "SupplierPayablePayments",
            columns: new[] { "SupplierId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierPayablePayments_CashbookEntryId",
            table: "SupplierPayablePayments",
            column: "CashbookEntryId");

        migrationBuilder.CreateTable(
            name: "SupplierPayablePaymentLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                PaymentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                GoodsReceiptId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierPayablePaymentLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierPayablePaymentLines_GoodsReceipts_GoodsReceiptId",
                    column: x => x.GoodsReceiptId,
                    principalTable: "GoodsReceipts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierPayablePaymentLines_SupplierPayablePayments_PaymentId",
                    column: x => x.PaymentId,
                    principalTable: "SupplierPayablePayments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierPayablePaymentLines_PaymentId",
            table: "SupplierPayablePaymentLines",
            column: "PaymentId");

        migrationBuilder.CreateTable(
            name: "SupplierPayableDiscounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                SupplierId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                PerformerUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Note = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                AllocateToDocuments = table.Column<bool>(type: "tinyint(1)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierPayableDiscounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierPayableDiscounts_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierPayableDiscounts_Users_PerformerUserId",
                    column: x => x.PerformerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierPayableDiscounts_Code",
            table: "SupplierPayableDiscounts",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierPayableDiscounts_SupplierId_OccurredAtUtc",
            table: "SupplierPayableDiscounts",
            columns: new[] { "SupplierId", "OccurredAtUtc" });

        migrationBuilder.CreateTable(
            name: "SupplierPayableDiscountLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                DiscountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                GoodsReceiptId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierPayableDiscountLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierPayableDiscountLines_GoodsReceipts_GoodsReceiptId",
                    column: x => x.GoodsReceiptId,
                    principalTable: "GoodsReceipts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierPayableDiscountLines_SupplierPayableDiscounts_DiscountId",
                    column: x => x.DiscountId,
                    principalTable: "SupplierPayableDiscounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierPayableDiscountLines_DiscountId",
            table: "SupplierPayableDiscountLines",
            column: "DiscountId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SupplierPayableDiscountLines");
        migrationBuilder.DropTable(name: "SupplierPayableDiscounts");
        migrationBuilder.DropTable(name: "SupplierPayablePaymentLines");
        migrationBuilder.DropTable(name: "SupplierPayablePayments");
        migrationBuilder.DropTable(name: "SupplierDebtAdjustments");

        migrationBuilder.DropColumn(
            name: "SupplierPayableDiscountPortion",
            table: "GoodsReceipts");
    }
}
