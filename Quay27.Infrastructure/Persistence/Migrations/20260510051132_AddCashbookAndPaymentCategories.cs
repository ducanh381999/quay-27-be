using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashbookAndPaymentCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashbookParties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Province = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ward = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashbookParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashbookParties_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaymentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kind = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CashbookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FundType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentCategoryId = table.Column<int>(type: "int", nullable: true),
                    AffectsBusinessResult = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CollectorUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CounterpartyScope = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CashbookPartyId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CounterpartyDisplayName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartnerDebtMode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceKind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    StaffUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashbookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashbookEntries_CashbookParties_CashbookPartyId",
                        column: x => x.CashbookPartyId,
                        principalTable: "CashbookParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashbookEntries_PaymentCategories_PaymentCategoryId",
                        column: x => x.PaymentCategoryId,
                        principalTable: "PaymentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashbookEntries_Users_CollectorUserId",
                        column: x => x.CollectorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashbookEntries_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashbookEntries_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_CashbookPartyId",
                table: "CashbookEntries",
                column: "CashbookPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_Code",
                table: "CashbookEntries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_CollectorUserId",
                table: "CashbookEntries",
                column: "CollectorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_CreatedByUserId",
                table: "CashbookEntries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_OccurredAtUtc",
                table: "CashbookEntries",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_PaymentCategoryId",
                table: "CashbookEntries",
                column: "PaymentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_SourceKind_SourceId",
                table: "CashbookEntries",
                columns: new[] { "SourceKind", "SourceId" },
                unique: true,
                filter: "`SourceId` IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_StaffUserId",
                table: "CashbookEntries",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookParties_CreatedByUserId",
                table: "CashbookParties",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCategories_Code",
                table: "PaymentCategories",
                column: "Code",
                unique: true);

            migrationBuilder.InsertData(
                table: "PaymentCategories",
                columns: new[] { "Id", "Code", "Name", "Kind" },
                values: new object[,]
                {
                    { 1, "ElectricExpense", "Chi phí điện", "Expense" },
                    { 2, "WaterExpense", "Chi phí nước", "Expense" },
                    { 3, "TelecomExpense", "Chi phí viễn thông", "Expense" },
                    { 4, "RentExpense", "Chi phí thuê kho bãi, mặt bằng kinh doanh", "Expense" },
                    { 5, "ManagementExpense", "Chi phí quản lý", "Expense" },
                    { 6, "LaborExpense", "Chi phí nhân công", "Expense" },
                    { 7, "TaxExpense", "Nộp thuế GTGT", "Expense" },
                    { 8, "OtherExpense", "Chi phí khác", "Expense" },
                    { 9, "PITTaxExpense", "Nộp thuế TNCN", "Expense" },
                    { 10, "OtherTaxExpense", "Nộp thuế khác", "Expense" },
                    { 11, "CustomerPayment", "Thu tiền khách trả", "Receipt" },
                    { 12, "OtherIncome", "Thu nhập khác", "Receipt" },
                });

            migrationBuilder.Sql("ALTER TABLE PaymentCategories AUTO_INCREMENT = 13;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashbookEntries");

            migrationBuilder.DropTable(
                name: "CashbookParties");

            migrationBuilder.DropTable(
                name: "PaymentCategories");
        }
    }
}
