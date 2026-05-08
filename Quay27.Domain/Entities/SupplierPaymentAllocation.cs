namespace Quay27.Domain.Entities;

public class SupplierPaymentAllocation
{
    public Guid Id { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public Guid? ReturnReceiptId { get; set; }
    public ReturnReceipt? ReturnReceipt { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public decimal Amount { get; set; }
    public Guid? ReceivingAccountId { get; set; }
    public ReceivingAccount? ReceivingAccount { get; set; }
}
