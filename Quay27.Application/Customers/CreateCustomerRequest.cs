using System.Text.Json.Serialization;

namespace Quay27.Application.Customers;

public record CreateCustomerRequest(
    int SortOrder,
    string InvoiceCode,
    DateTime BillCreatedAt,
    string NameAddress,
    string CreateMachine,
    string DraftStaff,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    string Quantity,
    string InstallStaffCm,
    bool ManagerApproved,
    bool Kio27Received,
    bool Export27,
    string Notes,
    string GoodsSenderNote,
    string AdditionalNotes,
    DateOnly SheetDate,
    string Status,
    bool FullSelfExport = false);
