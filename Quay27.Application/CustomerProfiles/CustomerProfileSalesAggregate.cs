namespace Quay27.Application.CustomerProfiles;

/// <summary>
/// Per-profile aggregates from SalesInvoices / SalesReturns (see CustomerProfileService for formulas).
/// </summary>
public readonly struct CustomerProfileSalesAggregate
{
    public CustomerProfileSalesAggregate(decimal totalPaid, decimal returnTotal, decimal debt)
    {
        TotalPaid = totalPaid;
        ReturnTotal = returnTotal;
        Debt = debt;
    }

    public decimal TotalPaid { get; }
    public decimal ReturnTotal { get; }
    public decimal Debt { get; }
}
