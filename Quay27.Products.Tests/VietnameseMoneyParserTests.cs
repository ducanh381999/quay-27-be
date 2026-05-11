using Quay27.Application.Dashboard;

namespace Quay27.Products.Tests;

public class VietnameseMoneyParserTests
{
    [Theory]
    [InlineData("120.000", 120_000)]
    [InlineData("1.234.567", 1_234_567)]
    [InlineData("140,5", 140.5)]
    [InlineData("  99  ", 99)]
    public void TryParseDecimal_accepts_common_vn_formats(string raw, decimal expected)
    {
        Assert.True(VietnameseMoneyParser.TryParseDecimal(raw, out var v));
        Assert.Equal(expected, v);
    }

    [Fact]
    public void TryParseDecimal_empty_is_false()
    {
        Assert.False(VietnameseMoneyParser.TryParseDecimal("", out _));
        Assert.False(VietnameseMoneyParser.TryParseDecimal("   ", out _));
    }
}
