using System.Globalization;

namespace Quay27.Application.Cashbook;

/// <summary>Formats and parses cashbook receipt codes: <c>PT-{EVENT}-{NNNNNN}</c>.</summary>
public static class CashbookCodeFormatting
{
    public const int ReceiptSequenceDigits = 6;
    public const int MaxEventSegmentLength = 24;
    public const string ReceiptPrefix = "PT";

    /// <summary>
    /// Builds a slug from <paramref name="raw"/> using only <c>[A-Z0-9_]</c>, uppercase, max length
    /// <see cref="MaxEventSegmentLength"/>. Empty input becomes <c>UNKNOWN</c>.
    /// </summary>
    public static string NormalizeReceiptEventSegment(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "UNKNOWN";

        Span<char> buffer = stackalloc char[MaxEventSegmentLength];
        var len = 0;
        foreach (var ch in raw.Trim())
        {
            if (len >= MaxEventSegmentLength)
                break;
            if (ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
            {
                buffer[len++] = ch == '_' ? '_' : char.ToUpperInvariant(ch);
            }
        }

        return len == 0 ? "UNKNOWN" : new string(buffer[..len]);
    }

    /// <summary>
    /// True when <paramref name="code"/> is <c>PT-{eventKey}-{dddddd}</c> with <paramref name="normalizedEventKey"/>
    /// matching <see cref="NormalizeReceiptEventSegment"/> output.
    /// </summary>
    public static bool TryParseReceiptSequence(string code, string normalizedEventKey, out int sequence)
    {
        sequence = 0;
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(normalizedEventKey))
            return false;

        var expected = $"{ReceiptPrefix}-{normalizedEventKey}-";
        if (!code.StartsWith(expected, StringComparison.Ordinal) || code.Length != expected.Length + ReceiptSequenceDigits)
            return false;

        var tail = code.AsSpan(expected.Length);
        return int.TryParse(tail, NumberStyles.None, CultureInfo.InvariantCulture, out sequence)
               && sequence >= 0;
    }
}
