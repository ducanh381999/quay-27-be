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

    [Fact]
    public void FormatGoodsReceiptSupplierPaymentCode_single_allocation()
    {
        var code = CashbookCodeFormatting.FormatGoodsReceiptSupplierPaymentCode("PN000004", 0, 1);
        Assert.Equal("PCPN000004", code);
    }

    [Fact]
    public void FormatGoodsReceiptSupplierPaymentCode_multiple_allocations()
    {
        Assert.Equal("PCPN000004-1", CashbookCodeFormatting.FormatGoodsReceiptSupplierPaymentCode("PN000004", 0, 2));
        Assert.Equal("PCPN000004-2", CashbookCodeFormatting.FormatGoodsReceiptSupplierPaymentCode("PN000004", 1, 2));
    }

    [Fact]
    public void FormatGoodsReceiptSupplierPaymentCode_empty_receipt_code_uses_unknown()
    {
        var code = CashbookCodeFormatting.FormatGoodsReceiptSupplierPaymentCode(null, 0, 1);
        Assert.Equal("PCUNKNOWN", code);
    }

    [Fact]
    public void FormatGoodsReceiptSupplierPaymentCode_respects_max_length()
    {
        var longRc = new string('X', 80);
        var code = CashbookCodeFormatting.FormatGoodsReceiptSupplierPaymentCode(longRc, 0, 1);
        Assert.True(code.Length <= CashbookCodeFormatting.MaxCashbookEntryCodeLength);
        Assert.StartsWith(CashbookCodeFormatting.SupplierGoodsReceiptPaymentPrefix, code, StringComparison.Ordinal);
    }
}
