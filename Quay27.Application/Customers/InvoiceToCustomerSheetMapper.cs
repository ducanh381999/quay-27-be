using System.Globalization;
using Quay27.Application.Common;
using Quay27.Domain.Entities;

namespace Quay27.Application.Customers;

/// <summary>Maps a new <see cref="SalesInvoice"/> to a <see cref="Customer"/> full-sheet row.</summary>
public static class InvoiceToCustomerSheetMapper
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string BuildNameAddress(SalesInvoice invoice, CustomerProfile? profile)
    {
        var name = !string.IsNullOrWhiteSpace(invoice.CustomerName)
            ? invoice.CustomerName.Trim()
            : profile?.CustomerName.Trim() ?? "Khách lẻ";

        if (profile is null)
            return name;

        var parts = new List<string> { name };
        void add(string? s)
        {
            var t = s?.Trim();
            if (!string.IsNullOrEmpty(t))
                parts.Add(t);
        }

        add(profile.Address);
        add(profile.Ward);
        add(profile.ProvinceCity);

        return string.Join(" — ", parts);
    }

    public static string FormatQuantity(SalesInvoice invoice)
    {
        var sum = invoice.Items.Sum(i => i.Quantity);
        return sum.ToString(Vi);
    }

    public static string FormatTotalAmount(SalesInvoice invoice)
    {
        var total = MoneyMath.Round(invoice.SubtotalAmount - invoice.DiscountAmount);
        return total.ToString("N0", Vi);
    }

    public static string TruncateStaffField(string? value, int maxLen = 128)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var t = value.Trim();
        return t.Length <= maxLen ? t : t[..maxLen];
    }

    public static Customer BuildCustomerRow(
        Guid customerRowId,
        SalesInvoice invoice,
        CustomerProfile? profile,
        int sortOrder,
        DateOnly sheetDate,
        string? sellerStaffLabel,
        string creatorUsername,
        DateTime utcNow,
        string createdByUsername)
    {
        var billAt = invoice.CreatedAtUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(invoice.CreatedAtUtc, DateTimeKind.Utc)
            : invoice.CreatedAtUtc.ToUniversalTime();

        var additional = string.IsNullOrWhiteSpace(invoice.Note)
            ? string.Empty
            : invoice.Note.Trim();

        return new Customer
        {
            Id = customerRowId,
            SalesInvoiceId = invoice.Id,
            SortOrder = sortOrder,
            InvoiceCode = invoice.Code.Trim(),
            BillCreatedAt = billAt,
            NameAddress = BuildNameAddress(invoice, profile),
            CreateMachine = TruncateStaffField(creatorUsername),
            DraftStaff = TruncateStaffField(sellerStaffLabel),
            Quantity = FormatQuantity(invoice),
            TotalAmount = FormatTotalAmount(invoice),
            InspectorStaff = string.Empty,
            InstallStaffCm = string.Empty,
            ManagerApproved = false,
            Kio27Received = false,
            Export27 = false,
            FullSelfExport = false,
            Notes = string.Empty,
            GoodsSenderNote = string.Empty,
            AdditionalNotes = additional,
            SheetDate = sheetDate,
            Status = "Mới",
            IsDuplicate = false,
            CreatedDate = utcNow,
            CreatedBy = createdByUsername,
            IsDeleted = false,
        };
    }
}
