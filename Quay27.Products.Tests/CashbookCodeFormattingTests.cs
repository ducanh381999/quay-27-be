using Quay27.Application.Cashbook;

namespace Quay27.Products.Tests;

public class CashbookCodeFormattingTests
{
    [Theory]
    [InlineData("CustomerPayment", "CUSTOMERPAYMENT")]
    [InlineData("  other-income  ", "OTHERINCOME")]
    [InlineData("AB-1", "AB1")]
    [InlineData("", "UNKNOWN")]
    [InlineData("   ", "UNKNOWN")]
    [InlineData("!!!", "UNKNOWN")]
    public void NormalizeReceiptEventSegment_trims_and_filters(string input, string expected) =>
        Assert.Equal(expected, CashbookCodeFormatting.NormalizeReceiptEventSegment(input));

    [Fact]
    public void NormalizeReceiptEventSegment_caps_length()
    {
        var longCode = new string('A', 40);
        Assert.Equal(24, CashbookCodeFormatting.NormalizeReceiptEventSegment(longCode).Length);
    }

    [Theory]
    [InlineData("PT-CUSTOMERPAYMENT-000001", "CUSTOMERPAYMENT", 1)]
    [InlineData("PT-SALESINVOICE-000042", "SALESINVOICE", 42)]
    [InlineData("PT-X-000000", "X", 0)]
    public void TryParseReceiptSequence_parses_valid_codes(string code, string ev, int seq)
    {
        Assert.True(CashbookCodeFormatting.TryParseReceiptSequence(code, ev, out var n));
        Assert.Equal(seq, n);
    }

    [Theory]
    [InlineData("PC000001", "CUSTOMERPAYMENT")]
    [InlineData("PT-CUSTOMERPAYMENT-1", "CUSTOMERPAYMENT")]
    [InlineData("PT-CUSTOMERPAYMENT-0000001", "CUSTOMERPAYMENT")]
    [InlineData("PT-WRONG-000001", "CUSTOMERPAYMENT")]
    public void TryParseReceiptSequence_rejects_invalid(string code, string ev) =>
        Assert.False(CashbookCodeFormatting.TryParseReceiptSequence(code, ev, out _));
}
