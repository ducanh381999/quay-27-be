namespace Quay27.Domain.Entities;

public sealed class SupplierPayablePaymentLine
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public SupplierPayablePayment? Payment { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public decimal Amount { get; set; }
}
