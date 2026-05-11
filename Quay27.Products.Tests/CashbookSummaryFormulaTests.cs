using Quay27.Application.Common;

namespace Quay27.Products.Tests;

public class CashbookSummaryFormulaTests
{
    [Fact]
    public void Closing_balance_is_rounded_opening_plus_receipts_minus_payments()
    {
        var opening = MoneyMath.Round(100.005m);
        var receipts = MoneyMath.Round(20.104m);
        var payments = MoneyMath.Round(5.991m);
        var closing = MoneyMath.Round(opening + receipts - payments);
        Assert.Equal(114.12m, closing);
    }
}
