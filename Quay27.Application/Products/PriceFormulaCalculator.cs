namespace Quay27.Application.Products;

public static class PriceFormulaCalculator
{
    public static decimal Calculate(
        decimal basePrice,
        string operation,
        decimal formulaValue,
        string unit,
        bool roundEnabled,
        int roundTo)
    {
        var delta = unit == "percent"
            ? basePrice * formulaValue / 100m
            : formulaValue;

        var raw = operation == "subtract"
            ? basePrice - delta
            : basePrice + delta;
        if (raw < 0) raw = 0;

        if (!roundEnabled || roundTo <= 1)
            return raw;

        return Math.Round(raw / roundTo, MidpointRounding.AwayFromZero) * roundTo;
    }
}
