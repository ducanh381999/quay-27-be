namespace Quay27.Application.Reports;

public static class EndOfDayReportTimeRange
{
    public static (DateTime FromUtc, DateTime ToUtc, string DateLabel) Resolve(EndOfDayReportQuery query)
    {
        if (string.Equals(query.TimeMode, "custom", StringComparison.OrdinalIgnoreCase))
        {
            var fromDate = ParseDate(query.CustomDateFrom) ?? DateTime.UtcNow.Date;
            var toDate = ParseDate(query.CustomDateTo) ?? fromDate;
            if (toDate < fromDate)
                (fromDate, toDate) = (toDate, fromDate);

            var fromUtc = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            var label = fromDate.Date == toDate.Date
                ? fromDate.ToString("dd/MM/yyyy")
                : $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
            return (fromUtc, toUtc, label);
        }

        var day = ParseDate(query.SingleDate) ?? DateTime.UtcNow.Date;
        var timeFrom = ParseTime(query.TimeFrom) ?? TimeSpan.Zero;
        var timeTo = ParseTime(query.TimeTo) ?? new TimeSpan(23, 59, 59);

        var fromLocal = day.Date.Add(timeFrom);
        var toLocal = day.Date.Add(timeTo);
        if (toLocal < fromLocal)
            toLocal = fromLocal;

        var fromUtcSingle = DateTime.SpecifyKind(fromLocal, DateTimeKind.Utc);
        var toUtcSingle = DateTime.SpecifyKind(toLocal, DateTimeKind.Utc);
        return (fromUtcSingle, toUtcSingle, day.ToString("dd/MM/yyyy"));
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTime.TryParse(value.Trim(), out var d) ? d.Date : null;
    }

    private static TimeSpan? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return TimeSpan.TryParse(value.Trim(), out var t) ? t : null;
    }
}
