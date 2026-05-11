using Quay27.Application.Customers;
using Quay27.Domain.Entities;

namespace Quay27.Products.Tests;

public class InvoiceToCustomerSheetMapperTests
{
    [Fact]
    public void BuildNameAddress_without_profile_uses_invoice_customer_name_trimmed()
    {
        var inv = new SalesInvoice { CustomerName = "  Nguyễn A  " };
        Assert.Equal("Nguyễn A", InvoiceToCustomerSheetMapper.BuildNameAddress(inv, null));
    }

    [Fact]
    public void BuildNameAddress_without_name_uses_khach_le()
    {
        var inv = new SalesInvoice { CustomerName = "   " };
        Assert.Equal("Khách lẻ", InvoiceToCustomerSheetMapper.BuildNameAddress(inv, null));
    }

    [Fact]
    public void BuildNameAddress_with_profile_joins_address_parts()
    {
        var inv = new SalesInvoice { CustomerName = "Shop" };
        var profile = new CustomerProfile
        {
            CustomerName = "IgnoredWhenInvoiceHasName",
            Address = "123 Đường X",
            Ward = "P1",
            ProvinceCity = "HCM",
        };
        var na = InvoiceToCustomerSheetMapper.BuildNameAddress(inv, profile);
        Assert.Contains("Shop", na);
        Assert.Contains("123 Đường X", na);
        Assert.Contains("P1", na);
        Assert.Contains("HCM", na);
    }

    [Fact]
    public void FormatQuantity_sums_line_quantities()
    {
        var inv = new SalesInvoice
        {
            Items =
            [
                new SalesInvoiceItem { Quantity = 2 },
                new SalesInvoiceItem { Quantity = 3 },
            ],
        };
        Assert.Equal("5", InvoiceToCustomerSheetMapper.FormatQuantity(inv));
    }

    [Fact]
    public void FormatTotalAmount_uses_subtotal_minus_discount_rounded()
    {
        var inv = new SalesInvoice { SubtotalAmount = 100.005m, DiscountAmount = 0.004m };
        var s = InvoiceToCustomerSheetMapper.FormatTotalAmount(inv);
        Assert.Equal("100", s);
    }

    [Fact]
    public void BuildCustomerRow_sets_sales_invoice_link_and_defaults()
    {
        var rowId = Guid.NewGuid();
        var invId = Guid.NewGuid();
        var inv = new SalesInvoice
        {
            Id = invId,
            Code = "INV-99",
            CreatedAtUtc = new DateTime(2026, 5, 10, 8, 30, 0, DateTimeKind.Utc),
            CustomerName = "C",
            SubtotalAmount = 200m,
            DiscountAmount = 50m,
            Note = "  Ghi chú đơn  ",
            Items = [new SalesInvoiceItem { Quantity = 1 }],
        };
        var sheetDate = new DateOnly(2026, 5, 10);
        var utc = DateTime.UtcNow;
        var row = InvoiceToCustomerSheetMapper.BuildCustomerRow(
            rowId,
            inv,
            null,
            sortOrder: 7,
            sheetDate,
            sellerStaffLabel: "Seller Name",
            creatorUsername: "creator1",
            utcNow: utc,
            createdByUsername: "creator1");

        Assert.Equal(rowId, row.Id);
        Assert.Equal(invId, row.SalesInvoiceId);
        Assert.Equal(7, row.SortOrder);
        Assert.Equal("INV-99", row.InvoiceCode);
        Assert.Equal(sheetDate, row.SheetDate);
        Assert.Equal("Mới", row.Status);
        Assert.Equal("Ghi chú đơn", row.AdditionalNotes);
        Assert.Equal("150", row.TotalAmount);
        Assert.Equal("1", row.Quantity);
        Assert.Equal("Seller Name", row.DraftStaff);
        Assert.Equal("creator1", row.CreateMachine);
        Assert.False(row.ManagerApproved);
    }

    [Fact]
    public void TruncateStaffField_limits_length()
    {
        var longText = new string('x', 200);
        Assert.Equal(128, InvoiceToCustomerSheetMapper.TruncateStaffField(longText).Length);
    }
}
