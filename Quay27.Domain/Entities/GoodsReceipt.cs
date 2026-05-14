namespace Quay27.Domain.Entities;

public class GoodsReceipt
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime ReceiptDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }

    /// <summary>Chiết khấu thanh toán đã phân bổ vào phiếu (giảm nợ phải trả theo phiếu).</summary>
    public decimal SupplierPayableDiscountPortion { get; set; }

    public decimal SupplierDebtDelta { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public List<GoodsReceiptLine> Lines { get; set; } = [];
    public List<SupplierPaymentAllocation> PaymentAllocations { get; set; } = [];
}
