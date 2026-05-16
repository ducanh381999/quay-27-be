namespace Quay27.Application.Reports;

public sealed class EndOfDaySummaryDto
{
    public IReadOnlyList<EndOfDayCashflowAggregateRowDto> CashflowRows { get; init; } =
        Array.Empty<EndOfDayCashflowAggregateRowDto>();

    public IReadOnlyList<EndOfDaySalesSummaryRowDto> SalesValueRows { get; init; } =
        Array.Empty<EndOfDaySalesSummaryRowDto>();

    public IReadOnlyList<EndOfDaySalesCountRowDto> SalesCountRows { get; init; } =
        Array.Empty<EndOfDaySalesCountRowDto>();
}

public sealed class EndOfDaySalesSummaryRowDto
{
    public string Label { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public decimal CashAmount { get; init; }
    public decimal TransferAmount { get; init; }
    public decimal CardAmount { get; init; }
}

public sealed class EndOfDaySalesCountRowDto
{
    public string Label { get; init; } = string.Empty;
    public int TransactionCount { get; init; }
    public decimal CashAmount { get; init; }
    public decimal TransferAmount { get; init; }
}
