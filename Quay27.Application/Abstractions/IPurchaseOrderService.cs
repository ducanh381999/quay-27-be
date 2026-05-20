using Quay27.Application.Cashbook;
using Quay27.Application.Orders;

namespace Quay27.Application.Abstractions;

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query,
        CancellationToken cancellationToken = default);

    Task<OrderCreatedDto> CreateAsync(CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrderLinkedInvoiceDto>> ListInvoicesAsync(Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrderCashbookRowDto>> ListCashbookEntriesAsync(Guid id,
        CancellationToken cancellationToken = default);

    Task PatchStatusAsync(Guid id, PatchOrderStatusRequest request, CancellationToken cancellationToken = default);
}
