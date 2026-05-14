using Quay27.Application.Services;

namespace Quay27.Products.Tests;

public class PurchasingReceiptStockHelperTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    [InlineData(100.0, 100)]
    public void LineQuantityToInt_accepts_whole_numbers(decimal q, int expected) =>
        Assert.Equal(expected, PurchasingReceiptStockHelper.LineQuantityToInt(q));

    [Fact]
    public void LineQuantityToInt_rejects_fractional()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PurchasingReceiptStockHelper.LineQuantityToInt(1.5m));
        Assert.Contains("nguyên", ex.Message);
    }

    [Fact]
    public void LineQuantityToInt_rejects_negative()
    {
        Assert.Throws<InvalidOperationException>(() => PurchasingReceiptStockHelper.LineQuantityToInt(-1m));
    }
}
