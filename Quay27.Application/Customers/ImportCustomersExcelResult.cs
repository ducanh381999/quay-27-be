namespace Quay27.Application.Customers;

public sealed record ImportCustomersExcelResult(
    int TotalRows,
    int ImportedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<string> Errors);
