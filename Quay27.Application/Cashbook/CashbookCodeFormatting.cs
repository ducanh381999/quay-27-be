using System.Globalization;

namespace Quay27.Application.Cashbook;

/// <summary>Formats and parses cashbook receipt codes: <c>PT-{EVENT}-{NNNNNN}</c>.</summary>
public static class CashbookCodeFormatting
{
    public const int ReceiptSequenceDigits = 6;
    public const int MaxEventSegmentLength = 24;
    public const string ReceiptPrefix = "PT";

    /// <summary>Prefix for supplier purchase payment lines tied to a goods receipt (<c>PC</c> + receipt code).</summary>
    public const string SupplierGoodsReceiptPaymentPrefix = "PC";

    public const int MaxCashbookEntryCodeLength = 64;

    /// <summary>
    /// Payment code for goods-receipt supplier allocations: <c>PC</c> + receipt code (e.g. <c>PCPN000004</c>).
    /// When <paramref name="allocationCount"/> is greater than 1, appends <c>-1</c>, <c>-2</c>, … for uniqueness.
    /// </summary>
    public static string FormatGoodsReceiptSupplierPaymentCode(
        string? goodsReceiptCode,
        int allocationIndex,
        int allocationCount)
    {
        var rc = string.IsNullOrWhiteSpace(goodsReceiptCode) ? "UNKNOWN" : goodsReceiptCode.Trim();
        var suffix = allocationCount <= 1 ? "" : $"-{allocationIndex + 1}";
        var reserved = SupplierGoodsReceiptPaymentPrefix.Length + suffix.Length;
        var maxReceiptLen = Math.Max(1, MaxCashbookEntryCodeLength - reserved);
        if (rc.Length > maxReceiptLen)
            rc = rc[..maxReceiptLen];

        return $"{SupplierGoodsReceiptPaymentPrefix}{rc}{suffix}";
    }

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
