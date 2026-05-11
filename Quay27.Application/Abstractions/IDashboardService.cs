using Quay27.Application.Dashboard;

namespace Quay27.Application.Abstractions;

public interface IDashboardService
{
    Task<TodaySummaryDto> GetTodaySummaryAsync(DateTime? utcNow, CancellationToken cancellationToken = default);

    Task<NetRevenueSeriesDto> GetNetRevenueSeriesAsync(
        DashboardPreset preset,
        DashboardBucket bucket,
        DateTime? utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopRankRowDto>> GetTopCustomersAsync(
        DashboardPreset preset,
        DateTime? utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopRankRowDto>> GetTopProductsAsync(
        DashboardPreset preset,
        DashboardProductMetric metric,
        DateTime? utcNow,
        CancellationToken cancellationToken = default);
}
