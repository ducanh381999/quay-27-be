namespace Quay27.Application.Customers;

public sealed record ImportCustomersExcelRequest(
    byte[] FileBytes,
    string FileName,
    DateOnly SheetDate);
