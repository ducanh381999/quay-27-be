using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Quay27CustomerSheetColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Name",
                table: "Customers");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceCode",
                table: "Customers",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "BillCreatedAt",
                table: "Customers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAddress",
                table: "Customers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CreateMachine",
                table: "Customers",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DraftStaff",
                table: "Customers",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InstallStaffCm",
                table: "Customers",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "ManagerApproved",
                table: "Customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Kio27Received",
                table: "Customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Export27",
                table: "Customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Customers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GoodsSenderNote",
                table: "Customers",
                type: "varchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalNotes",
                table: "Customers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE `Customers`
                SET
                  `InvoiceCode` = LEFT(TRIM(COALESCE(`Phone`, '')), 64),
                  `BillCreatedAt` = `CreatedDate`,
                  `NameAddress` = NULLIF(TRIM(CONCAT(COALESCE(`Name`, ''), ' ', COALESCE(`Address`, ''))), ''),
                  `Notes` = COALESCE(`Note`, ''),
                  `AdditionalNotes` = ''
                WHERE 1 = 1;
                """);

            migrationBuilder.Sql("""
                UPDATE `Customers`
                SET `NameAddress` = COALESCE(`NameAddress`, TRIM(COALESCE(`Name`, '')), '-')
                WHERE `NameAddress` IS NULL OR `NameAddress` = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceCode",
                table: "Customers",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BillCreatedAt",
                table: "Customers",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NameAddress",
                table: "Customers",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Customers",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalNotes",
                table: "Customers",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_InvoiceCode",
                table: "Customers",
                column: "InvoiceCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_InvoiceCode",
                table: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Customers",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Customers",
                type: "varchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Customers",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE `Customers`
                SET
                  `Phone` = LEFT(TRIM(COALESCE(`InvoiceCode`, '')), 64),
                  `Note` = LEFT(COALESCE(`Notes`, ''), 4000),
                  `Name` = LEFT(COALESCE(`NameAddress`, ''), 512),
                  `Address` = CASE
                    WHEN CHAR_LENGTH(COALESCE(`NameAddress`, '')) > 512
                    THEN SUBSTRING(COALESCE(`NameAddress`, ''), 513)
                    ELSE ''
                  END;
                """);

            migrationBuilder.DropColumn(
                name: "AdditionalNotes",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BillCreatedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreateMachine",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DraftStaff",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Export27",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "GoodsSenderNote",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "InstallStaffCm",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "InvoiceCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Kio27Received",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ManagerApproved",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NameAddress",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name");
        }
    }
}
