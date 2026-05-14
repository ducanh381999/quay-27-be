namespace Quay27.Domain.Entities;

public sealed class SupplierPayableDiscountLine
{
    public Guid Id { get; set; }
    public Guid DiscountId { get; set; }
    public SupplierPayableDiscount? Discount { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public decimal Amount { get; set; }
}
