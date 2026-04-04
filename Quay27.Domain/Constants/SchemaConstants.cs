using System.Reflection;
using Quay27.Domain.Entities;

namespace Quay27.Domain.Constants;

public static class SchemaConstants
{
    public const string CustomersTable = "Customers";

    public static class CustomerColumns
    {
        public const string SortOrder = nameof(Customer.SortOrder);
        public const string InvoiceCode = nameof(Customer.InvoiceCode);
        public const string BillCreatedAt = nameof(Customer.BillCreatedAt);
        public const string NameAddress = nameof(Customer.NameAddress);
        public const string CreateMachine = nameof(Customer.CreateMachine);
        public const string DraftStaff = nameof(Customer.DraftStaff);
        public const string Quantity = nameof(Customer.Quantity);
        public const string InstallStaffCm = nameof(Customer.InstallStaffCm);
        public const string ManagerApproved = nameof(Customer.ManagerApproved);
        public const string Kio27Received = nameof(Customer.Kio27Received);
        public const string Export27 = nameof(Customer.Export27);
        public const string FullSelfExport = nameof(Customer.FullSelfExport);
        public const string Notes = nameof(Customer.Notes);
        public const string GoodsSenderNote = nameof(Customer.GoodsSenderNote);
        public const string AdditionalNotes = nameof(Customer.AdditionalNotes);
        public const string SheetDate = nameof(Customer.SheetDate);
        public const string Status = nameof(Customer.Status);
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
    }

    /// <summary>Queue id seeded for &quot;Quầy 27&quot; — tick CẤP 27 in full sheet enrolls here.</summary>
    public const int Quay27QueueId = 1;

    /// <summary>Giá trị <see cref="Customer.Notes"/> khi hóa đơn hủy — đồng bộ BE/FE.</summary>
    public const string CancelledInvoiceNotes = "Hủy hóa đơn";

    /// <summary>Allowlisted <see cref="Customer"/> property names for <see cref="CustomersTable"/> column permissions.</summary>
    public static IReadOnlyList<string> GetAllCustomerColumnNames() =>
        typeof(CustomerColumns)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();
}
