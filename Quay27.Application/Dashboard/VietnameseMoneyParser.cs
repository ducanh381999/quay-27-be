using System.Globalization;

namespace Quay27.Application.Dashboard;

public static class VietnameseMoneyParser
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>Parse chuỗi tiền sheet (thường dạng VN: dấu phân tách nghìn / thập phân).</summary>
    public static bool TryParseDecimal(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var s = raw.Trim().Replace("\u00a0", "").Replace(" ", "");

        if (decimal.TryParse(s, NumberStyles.Number, Vi, out value))
            return true;

        // Fallback: chỉ giữ số, dấu trừ, dấu phẩy/chấm cuối là thập phân
        var cleaned = new string(s.Where(ch => char.IsDigit(ch) || ch is '.' or ',' or '-').ToArray());
        if (cleaned.Length == 0)
            return false;

        // Nếu có cả . và , coi , là thập phân kiểu VN
        if (cleaned.Contains(',') && cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(".", "").Replace(',', '.');
        }
        else if (cleaned.Contains(','))
        {
            var parts = cleaned.Split(',');
            if (parts.Length == 2 && parts[1].Length <= 2)
                cleaned = cleaned.Replace(",", ".");
            else
                cleaned = cleaned.Replace(",", "");
        }
        else
            cleaned = cleaned.Replace(".", "");

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    public static decimal ParseDecimalOrZero(string? raw) =>
        TryParseDecimal(raw, out var v) ? v : 0;
}
