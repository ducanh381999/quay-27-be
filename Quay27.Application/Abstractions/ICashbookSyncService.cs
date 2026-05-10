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
}
