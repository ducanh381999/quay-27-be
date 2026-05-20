namespace Quay27.Application.Reports;

public sealed class EndOfDayCashflowRowDto
{
    public string Code { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public string EntryType { get; init; } = string.Empty;
    public string? CategoryName { get; init; }
    public string? StaffDisplayName { get; init; }
    public string? CounterpartyName { get; init; }
    public decimal Amount { get; init; }
    public string? SourceCode { get; init; }
}

public sealed class EndOfDayCashflowAggregateRowDto
{
    public string Label { get; init; } = string.Empty;
    public decimal CashAmount { get; init; }
    public decimal TransferAmount { get; init; }
    public decimal CardAmount { get; init; }
}
