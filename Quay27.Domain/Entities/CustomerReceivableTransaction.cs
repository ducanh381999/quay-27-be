namespace Quay27.Domain.Entities;

/// <summary>CRM công nợ khách: thanh toán, điều chỉnh, chiết khấu, QR (v1 không phân bổ HĐ).</summary>
public sealed class CustomerReceivableTransaction
{
    public Guid Id { get; set; }
    public Guid CustomerProfileId { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }

    /// <summary>payment | adjustment | payment_discount | qr_payment</summary>
    public string Kind { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Thanh toán/chiết khấu/QR: số tiền làm giảm nợ. Điều chỉnh: có thể 0 (chỉ đổi số dư qua balance).</summary>
    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public string? PaymentMethod { get; set; }
    public string? CollectorOrPerformerName { get; set; }
    public string? Note { get; set; }
    public string? Description { get; set; }

    public Guid? ReceivingAccountId { get; set; }
    public ReceivingAccount? ReceivingAccount { get; set; }

    public bool AllocateToInvoice { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
