namespace Quay27.Application.Reports;

public sealed class ReportMetaDto
{
    public string Title { get; init; } = string.Empty;
    public string DateLabel { get; init; } = string.Empty;
    public string BranchLabel { get; init; } = "Chi nhánh trung tâm";
    public DateTime GeneratedAtLocal { get; init; }
    public string DisplayMode { get; init; } = "vertical";
}

public sealed class ReportDataDto<TRow>
{
    public ReportMetaDto Meta { get; init; } = new();
    public IReadOnlyList<TRow> Rows { get; init; } = Array.Empty<TRow>();
}
