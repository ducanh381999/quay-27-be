namespace Quay27.Application.Common;

/// <summary>Calendar date in Vietnam (Asia/Ho_Chi_Minh) for sheet / carryover logic.</summary>
public static class VietnamDate
{
    public static DateOnly TodayInVietnam()
    {
        var tz = GetVietnamTimeZone();
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return DateOnly.FromDateTime(local);
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new InvalidOperationException("Vietnam time zone not found on this system.");
    }
}
