namespace Quay27.Application.Suppliers;

public record ImportSuppliersExcelRequest(byte[] FileBytes, string? FileName, bool UpdateClosingDebt);

public record ImportSuppliersExcelResult(
    int TotalRows,
    int ImportedCount,
    int UpdatedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<string> Errors);
