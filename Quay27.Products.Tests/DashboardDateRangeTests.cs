using Quay27.Application.Dashboard;

namespace Quay27.Products.Tests;

public class DashboardDateRangeTests
{
    [Fact]
    public void Resolve_today_is_single_day()
    {
        var utc = new DateTime(2026, 5, 11, 10, 0, 0, DateTimeKind.Utc);
        var r = DashboardDateRange.Resolve(DashboardPreset.Today, utc);
        Assert.Equal(r.From, r.To);
    }

    [Fact]
    public void ToUtcHalfOpenInterval_end_is_exclusive()
    {
        var utc = new DateTime(2026, 5, 11, 10, 0, 0, DateTimeKind.Utc);
        var r = DashboardDateRange.Resolve(DashboardPreset.Today, utc);
        var (start, endEx) = DashboardDateRange.ToUtcHalfOpenInterval(r, utc);
        Assert.True(start < endEx);
    }

    [Theory]
    [InlineData("today", DashboardPreset.Today)]
    [InlineData("LAST7DAYS", DashboardPreset.Last7Days)]
    [InlineData("thismonth", DashboardPreset.ThisMonth)]
    public void TryParsePreset_maps_query_strings(string raw, DashboardPreset expected)
    {
        var p = DashboardDateRange.TryParsePreset(raw);
        Assert.True(p.HasValue);
        Assert.Equal(expected, p.Value);
    }

    [Fact]
    public void TryParsePreset_invalid_returns_null()
    {
        Assert.False(DashboardDateRange.TryParsePreset("invalid").HasValue);
    }
}
