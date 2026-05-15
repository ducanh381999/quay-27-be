using Quay27.Application.Orders;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IPurchaseOrderRepository
{
    Task<IReadOnlyList<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query,
        CancellationToken cancellationToken = default);

    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default);

    Task AddAsync(PurchaseOrder entity, CancellationToken cancellationToken = default);

    Task<PurchaseOrder?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PurchaseOrder?> GetByIdNoTrackingAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Sum of line quantities on non-terminal purchase orders for this product (draft/confirmed/shipping).</summary>
    Task<int> SumReservedQuantityForProductInOpenOrdersAsync(Guid productId,
        CancellationToken cancellationToken = default);
}
