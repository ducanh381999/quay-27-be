namespace Quay27.Domain.Entities;

/// <summary>Thanh toán cho nhà cung cấp (có thể phân bổ phiếu nhập).</summary>
public sealed class SupplierPayablePayment
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid PayerUserId { get; set; }
    public User? PayerUser { get; set; }
    /// <summary>cash | card | bankTransfer</summary>
    public string PaymentMethod { get; set; } = "cash";
    public Guid? ReceivingAccountId { get; set; }
    public ReceivingAccount? ReceivingAccount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public bool AllocateToDocuments { get; set; }
    /// <summary>Thanh toán ghi sổ quỹ (loại 7).</summary>
    public bool PostToCashbook { get; set; }
    public Guid? CashbookEntryId { get; set; }
    public CashbookEntry? CashbookEntry { get; set; }
    public List<SupplierPayablePaymentLine> Lines { get; set; } = [];
}
