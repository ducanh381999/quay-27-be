namespace Quay27.Domain.Entities;

/// <summary>Chiết khấu thanh toán từ nhà cung cấp.</summary>
public sealed class SupplierPayableDiscount
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid PerformerUserId { get; set; }
    public User? PerformerUser { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public bool AllocateToDocuments { get; set; }
    public List<SupplierPayableDiscountLine> Lines { get; set; } = [];
}
