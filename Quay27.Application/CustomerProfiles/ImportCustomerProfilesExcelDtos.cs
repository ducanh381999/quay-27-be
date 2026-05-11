namespace Quay27.Application.CustomerProfiles;

public sealed record ImportCustomerProfilesExcelRequest(
    byte[] FileBytes,
    string? FileName,
    bool SkipDuplicatesByPhone);

public sealed record ImportCustomerProfilesExcelResult(
    int TotalRows,
    int ImportedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<string> Errors);
