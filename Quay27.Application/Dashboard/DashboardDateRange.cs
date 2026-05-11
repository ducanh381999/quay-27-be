namespace Quay27.Application.Dashboard;

/// <summary>Giống subset preset FE: today, yesterday, last7Days, thisMonth, lastMonth.</summary>
public enum DashboardPreset
{
    Today,
    Yesterday,
    Last7Days,
    ThisMonth,
    LastMonth,
}

public readonly record struct DateRangeYmd(DateOnly From, DateOnly To);

/// <summary>Khoảng ngày theo lịch Việt Nam (+07), khớp logic <c>resolvePreset</c> phía Next.js.</summary>
public static class DashboardDateRange
{
    private static DateOnly TodayVn(DateTime utcNow)
    {
        var vn = TimeZoneInfo.ConvertTimeFromUtc(utcNow, VietnamTimeZone());
        return DateOnly.FromDateTime(vn.Date);
    }

    public static TimeZoneInfo VietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    public static DateRangeYmd Resolve(DashboardPreset preset, DateTime utcNow)
    {
        var today = TodayVn(utcNow);
        return preset switch
        {
            DashboardPreset.Today => new DateRangeYmd(today, today),
            DashboardPreset.Yesterday => new DateRangeYmd(today.AddDays(-1), today.AddDays(-1)),
            DashboardPreset.Last7Days => new DateRangeYmd(today.AddDays(-6), today),
            DashboardPreset.ThisMonth => new DateRangeYmd(
                new DateOnly(today.Year, today.Month, 1),
                new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))),
            DashboardPreset.LastMonth =>
                today.Month == 1
                    ? new DateRangeYmd(
                        new DateOnly(today.Year - 1, 12, 1),
                        new DateOnly(today.Year - 1, 12, 31))
                    : new DateRangeYmd(
                        new DateOnly(today.Year, today.Month - 1, 1),
                        new DateOnly(today.Year, today.Month - 1,
                            DateTime.DaysInMonth(today.Year, today.Month - 1))),
            _ => new DateRangeYmd(today, today),
        };
    }

    /// <summary>BillCreatedAt lưu UTC — chuyển cạnh trái/phải khoảng ngày VN sang UTC để filter SQL.</summary>
    public static (DateTime StartUtc, DateTime EndExclusiveUtc) ToUtcHalfOpenInterval(
        DateRangeYmd range,
        DateTime utcNow)
    {
        var tz = VietnamTimeZone();
        var startVn = range.From.ToDateTime(TimeOnly.MinValue);
        var startUnspec = DateTime.SpecifyKind(startVn, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startUnspec, tz);

        var endVnExclusive = range.To.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var endUnspec = DateTime.SpecifyKind(endVnExclusive, DateTimeKind.Unspecified);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endUnspec, tz);

        return (startUtc, endUtc);
    }

    public static DateTime ToVietnamWallFromBillUtc(DateTime billUtc)
    {
        var utc = billUtc.Kind == DateTimeKind.Utc
            ? billUtc
            : DateTime.SpecifyKind(billUtc.ToUniversalTime(), DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone());
    }

    public static DashboardPreset? TryParsePreset(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        return raw.Trim().ToLowerInvariant() switch
        {
            "today" => DashboardPreset.Today,
            "yesterday" => DashboardPreset.Yesterday,
            "last7days" => DashboardPreset.Last7Days,
            "thismonth" => DashboardPreset.ThisMonth,
            "lastmonth" => DashboardPreset.LastMonth,
            _ => null,
        };
    }
}
