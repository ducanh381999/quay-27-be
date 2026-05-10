namespace Quay27.Application.Common;

public static class MoneyMath
{
    public static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static bool EqualsMoney(decimal a, decimal b) => Round(a) == Round(b);
}
