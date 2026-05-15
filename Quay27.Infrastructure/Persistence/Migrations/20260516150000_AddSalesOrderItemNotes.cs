using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quay27.Infrastructure.Persistence;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260516150000_AddSalesOrderItemNotes")]
public class AddSalesOrderItemNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Note",
            table: "SalesInvoiceItems",
            type: "varchar(2000)",
            maxLength: 2000,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "Note",
            table: "SalesReturnItems",
            type: "varchar(2000)",
            maxLength: 2000,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "Note",
            table: "SalesReturnExchangeItems",
            type: "varchar(2000)",
            maxLength: 2000,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Note", table: "SalesInvoiceItems");
        migrationBuilder.DropColumn(name: "Note", table: "SalesReturnItems");
        migrationBuilder.DropColumn(name: "Note", table: "SalesReturnExchangeItems");
    }
}
