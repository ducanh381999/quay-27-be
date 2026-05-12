using Quay27.Domain.Entities;

namespace Quay27.Application.CustomerProfiles;

/// <summary>
/// One profile row with invoice/return aggregates for the CRM list query (single round-trip).
/// </summary>
public sealed class CustomerProfileListPageRow
{
    public required CustomerProfile Profile { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal ReturnTotal { get; init; }
    public decimal InvoiceDebt { get; init; }
}
