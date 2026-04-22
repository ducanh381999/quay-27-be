using Quay27.Application.Products;

namespace Quay27.Products.Tests;

public class PriceFormulaCalculatorTests
{
    [Theory]
    [InlineData(100000, "add", 5000, "vnd", false, 1, 105000)]
    [InlineData(100000, "subtract", 5000, "vnd", false, 1, 95000)]
    [InlineData(100000, "add", 10, "percent", false, 1, 110000)]
    [InlineData(100000, "subtract", 10, "percent", false, 1, 90000)]
    [InlineData(101111, "add", 0, "vnd", true, 1000, 101000)]
    [InlineData(101501, "add", 0, "vnd", true, 1000, 102000)]
    [InlineData(1000, "subtract", 5000, "vnd", false, 1, 0)]
    public void Calculate_should_return_expected_value(
        decimal basePrice,
        string operation,
        decimal formulaValue,
        string unit,
        bool roundEnabled,
        int roundTo,
        decimal expected)
    {
        var actual = PriceFormulaCalculator.Calculate(
            basePrice,
            operation,
            formulaValue,
            unit,
            roundEnabled,
            roundTo);

        Assert.Equal(expected, actual);
    }
}
