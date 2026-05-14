using Quay27.Domain.Entities;

namespace Quay27.Application.Abstractions;

public interface ICashbookSyncService
{
    /// <summary>Đồng bộ sau khi hóa đơn ở trạng thái completed (idempotent).</summary>
    Task SyncAfterSalesInvoiceStatusAsync(SalesInvoice invoice, string? previousStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Đồng bộ sau khi đặt hàng ở trạng thái completed (idempotent).</summary>
    Task SyncAfterPurchaseOrderStatusAsync(PurchaseOrder order, string? previousStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Đồng bộ sau khi trả hàng ở trạng thái returned (idempotent).</summary>
    Task SyncAfterSalesReturnStatusAsync(SalesReturn salesReturn, string? previousStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Xóa sổ quỹ theo allocation cũ, rồi ghi lại chi theo từng khoản thanh toán khi phiếu nhập ở trạng thái received.</summary>
    Task SyncGoodsReceiptSupplierPaymentsAsync(GoodsReceipt receipt,
        IReadOnlyList<Guid> previousPaymentAllocationIds, CancellationToken cancellationToken = default);

    /// <summary>Xóa sổ quỹ theo allocation cũ, rồi ghi lại thu theo từng khoản khi phiếu trả hàng nhập ở trạng thái returned.</summary>
    Task SyncReturnReceiptSupplierPaymentsAsync(ReturnReceipt receipt,
        IReadOnlyList<Guid> previousPaymentAllocationIds, CancellationToken cancellationToken = default);
}
