namespace Quay27.Domain.Entities;

/// <summary>Một dòng sổ quỹ: phiếu thu hoặc phiếu chi.</summary>
public sealed class CashbookEntry
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;

    /// <summary>Receipt | Payment</summary>
    public string EntryType { get; set; } = "Receipt";

    /// <summary>cash | bank | ewallet</summary>
    public string FundType { get; set; } = "cash";

    public DateTime OccurredAtUtc { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }

    public int? PaymentCategoryId { get; set; }
    public PaymentCategory? PaymentCategory { get; set; }

    public bool AffectsBusinessResult { get; set; }

    /// <summary>paid | cancelled</summary>
    public string Status { get; set; } = "paid";

    public Guid? CollectorUserId { get; set; }
    public User? CollectorUser { get; set; }

    /// <summary>other | customer | supplier</summary>
    public string CounterpartyScope { get; set; } = "other";

    public Guid? CashbookPartyId { get; set; }
    public CashbookParty? CashbookParty { get; set; }
    public string? CounterpartyDisplayName { get; set; }

    /// <summary>include_in_debt | exclude_from_debt | no_debt | not_applicable</summary>
    public string PartnerDebtMode { get; set; } = "not_applicable";

    /// <summary>Manual | SalesInvoice | PurchaseOrder | SalesReturn</summary>
    public string SourceKind { get; set; } = "Manual";

    public Guid? SourceId { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public Guid? StaffUserId { get; set; }
    public User? StaffUser { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
