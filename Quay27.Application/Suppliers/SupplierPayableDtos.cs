namespace Quay27.Application.Suppliers;

/// <summary>Bộ lọc / nhãn loại giao dịch công nợ NCC (theo spec UI).</summary>
public static class SupplierPayableTransactionTypes
{
    public const int Payment = 0;
    public const int Adjustment = 1;
    public const int GoodsReceipt = 2;
    public const int Sales = 3;
    public const int SalesReturn = 4;
    public const int SupplierReturn = 5;
    public const int AdvanceRefund = 6;
    public const int CashbookPayment = 7;
    public const int Other = 8;
    public const int PointPayment = 9;
    public const int Undelivered = 10;
    public const int OpeningDebt = 11;
    public const int Delivery = 12;
    public const int PurchaseOrder = 13;
    public const int Payroll = 14;
    public const int PaymentDiscount = 15;
    public const int Refund = 17;
    public const int ServicePurchase = 23;
    public const int ServicePurchasePayment = 24;

    public static string GetLabel(int type) => type switch
    {
        Payment => "Thanh toán",
        Adjustment => "Điều chỉnh",
        GoodsReceipt => "Nhập hàng",
        Sales => "Bán hàng",
        SalesReturn => "Trả hàng",
        SupplierReturn => "Trả hàng nhà cung cấp",
        AdvanceRefund => "Hoàn trả tạm ứng",
        CashbookPayment => "Thanh toán(Sổ quỹ)",
        Other => "Khác",
        PointPayment => "Thanh toán bằng điểm",
        Undelivered => "Không giao được hàng",
        OpeningDebt => "Dư nợ đầu kỳ",
        Delivery => "Giao hàng",
        PurchaseOrder => "Đặt hàng nhập",
        Payroll => "Phiếu lương",
        PaymentDiscount => "Chiết khấu thanh toán",
        Refund => "Hoàn tiền",
        ServicePurchase => "Mua dịch vụ",
        ServicePurchasePayment => "Thanh toán mua dịch vụ",
        _ => "Khác",
    };

    public static IReadOnlyList<(int Value, string Label)> AllFilterOptions()
    {
        var seen = new HashSet<int>();
        var list = new List<(int, string)>();
        foreach (var v in new[]
                 {
                     Payment, Adjustment, GoodsReceipt, Sales, SalesReturn, SupplierReturn, AdvanceRefund,
                     CashbookPayment, Other, PointPayment, Undelivered, OpeningDebt, Delivery, PurchaseOrder,
                     Payroll, PaymentDiscount, Refund, ServicePurchase, ServicePurchasePayment,
                 })
        {
            if (seen.Add(v))
                list.Add((v, GetLabel(v)));
        }

        return list;
    }
}

public sealed record SupplierPayableTransactionDto(
    Guid Id,
    string SourceKind,
    string Code,
    DateTime OccurredAtUtc,
    int TransactionType,
    string TransactionTypeLabel,
    decimal ValueAmount,
    /// <summary>Ảnh hưởng tới nợ phải trả NCC: dương = tăng nợ, âm = giảm nợ.</summary>
    decimal PayableDebtImpact);

public sealed record SupplierOpenGoodsReceiptRowDto(
    Guid Id,
    string Code,
    DateTime ReceiptDate,
    decimal Total,
    decimal PaidAmount,
    /// <summary>Số nợ còn phải trả theo phiếu (sau trả trước & chiết khấu phân bổ).</summary>
    decimal RemainingDebt);

public sealed record SupplierDebtAdjustmentDto(
    Guid Id,
    string Code,
    DateTime OccurredAtUtc,
    decimal Delta,
    string Description);

public sealed record SupplierPayablePaymentDto(
    Guid Id,
    string Code,
    DateTime OccurredAtUtc,
    decimal TotalAmount,
    bool PostToCashbook);

public sealed record SupplierPayableDiscountDto(
    Guid Id,
    string Code,
    DateTime OccurredAtUtc,
    decimal TotalAmount);

public sealed record CreateSupplierDebtAdjustmentRequest(
    DateTime? OccurredAtUtc,
    decimal Delta,
    string? Description);

public sealed record SupplierPayablePaymentLineRequest(Guid? GoodsReceiptId, decimal Amount);

public sealed record CreateSupplierPayablePaymentRequest(
    DateTime? OccurredAtUtc,
    Guid PayerUserId,
    string PaymentMethod,
    Guid? ReceivingAccountId,
    decimal Amount,
    string? Note,
    bool AllocateToDocuments,
    bool PostToCashbook,
    int? CashbookPaymentCategoryId,
    IReadOnlyList<SupplierPayablePaymentLineRequest>? Lines);

public sealed record SupplierPayableDiscountLineRequest(Guid GoodsReceiptId, decimal Amount);

public sealed record CreateSupplierPayableDiscountRequest(
    DateTime? OccurredAtUtc,
    Guid PerformerUserId,
    decimal Amount,
    string? Note,
    bool AllocateToDocuments,
    IReadOnlyList<SupplierPayableDiscountLineRequest>? Lines);
