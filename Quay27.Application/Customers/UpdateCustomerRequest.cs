namespace Quay27.Application.Customers;

public record UpdateCustomerRequest(
    int? SortOrder,
    string? InvoiceCode,
    DateTime? BillCreatedAt,
    string? NameAddress,
    string? CreateMachine,
    string? DraftStaff,
    int? Quantity,
    string? InstallStaffCm,
    bool? ManagerApproved,
    bool? Kio27Received,
    bool? Export27,
    bool? FullSelfExport,
    string? Notes,
    string? GoodsSenderNote,
    string? AdditionalNotes,
    DateOnly? SheetDate,
    string? Status);
