using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerDeliveryReceivable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerDeliveryAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AddressName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipientName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AddressLine = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProvinceCity = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ward = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDeliveryAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerDeliveryAddresses_CustomerProfiles_CustomerProfileId",
                        column: x => x.CustomerProfileId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerReceivableTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CollectorOrPerformerName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceivingAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    AllocateToInvoice = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReceivableTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReceivableTransactions_CustomerProfiles_CustomerProf~",
                        column: x => x.CustomerProfileId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerReceivableTransactions_ReceivingAccounts_ReceivingAc~",
                        column: x => x.ReceivingAccountId,
                        principalTable: "ReceivingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDeliveryAddresses_CustomerProfileId_IsDeleted",
                table: "CustomerDeliveryAddresses",
                columns: new[] { "CustomerProfileId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReceivableTransactions_CustomerProfileId_CreatedAtUtc",
                table: "CustomerReceivableTransactions",
                columns: new[] { "CustomerProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReceivableTransactions_ReceivingAccountId",
                table: "CustomerReceivableTransactions",
                column: "ReceivingAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerDeliveryAddresses");

            migrationBuilder.DropTable(
                name: "CustomerReceivableTransactions");
        }
    }
}
