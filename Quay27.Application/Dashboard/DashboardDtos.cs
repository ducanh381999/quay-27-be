namespace Quay27.Application.Dashboard;

public enum DashboardBucket
{
    Hour,
    Day,
    Weekday,
}

public enum DashboardProductMetric
{
    NetRevenue,
    Quantity,
}

public sealed record TodaySummaryDto(
    decimal RevenueAmount,
    int InvoiceCount,
    decimal ReturnAmount,
    int ReturnOrderCount);

public sealed record ChartPointDto(string Label, decimal Value);

public sealed record NetRevenueSeriesDto(decimal Total, IReadOnlyList<ChartPointDto> Points);

public sealed record TopRankRowDto(string Name, decimal Value);
