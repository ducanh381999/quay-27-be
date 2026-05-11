using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Dashboard;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(TodaySummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TodaySummaryDto>> Today(CancellationToken cancellationToken) =>
        Ok(await _dashboard.GetTodaySummaryAsync(null, cancellationToken));

    [HttpGet("net-revenue")]
    [ProducesResponseType(typeof(NetRevenueSeriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NetRevenueSeriesDto>> NetRevenue(
        [FromQuery] string preset,
        [FromQuery] string bucket,
        CancellationToken cancellationToken)
    {
        if (DashboardDateRange.TryParsePreset(preset) is not { } p)
            return BadRequest("Invalid preset.");
        if (!TryParseBucket(bucket, out var b))
            return BadRequest("Invalid bucket.");
        return Ok(await _dashboard.GetNetRevenueSeriesAsync(p, b, null, cancellationToken));
    }

    [HttpGet("top-customers")]
    [ProducesResponseType(typeof(IReadOnlyList<TopRankRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TopRankRowDto>>> TopCustomers(
        [FromQuery] string preset,
        CancellationToken cancellationToken)
    {
        if (DashboardDateRange.TryParsePreset(preset) is not { } p)
            return BadRequest("Invalid preset.");
        return Ok(await _dashboard.GetTopCustomersAsync(p, null, cancellationToken));
    }

    [HttpGet("top-products")]
    [ProducesResponseType(typeof(IReadOnlyList<TopRankRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TopRankRowDto>>> TopProducts(
        [FromQuery] string preset,
        [FromQuery] string metric,
        CancellationToken cancellationToken)
    {
        if (DashboardDateRange.TryParsePreset(preset) is not { } p)
            return BadRequest("Invalid preset.");
        if (!TryParseProductMetric(metric, out var m))
            return BadRequest("Invalid metric.");
        return Ok(await _dashboard.GetTopProductsAsync(p, m, null, cancellationToken));
    }

    private static bool TryParseBucket(string? raw, out DashboardBucket bucket)
    {
        bucket = DashboardBucket.Day;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        switch (raw.Trim().ToLowerInvariant())
        {
            case "hour":
                bucket = DashboardBucket.Hour;
                return true;
            case "day":
                bucket = DashboardBucket.Day;
                return true;
            case "weekday":
                bucket = DashboardBucket.Weekday;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseProductMetric(string? raw, out DashboardProductMetric metric)
    {
        metric = DashboardProductMetric.NetRevenue;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        switch (raw.Trim().ToLowerInvariant())
        {
            case "netrevenue":
            case "revenue":
                metric = DashboardProductMetric.NetRevenue;
                return true;
            case "quantity":
            case "qty":
                metric = DashboardProductMetric.Quantity;
                return true;
            default:
                return false;
        }
    }
}
