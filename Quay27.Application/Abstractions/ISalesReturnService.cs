using Quay27.Application.Cashbook;
using Quay27.Application.Orders;

namespace Quay27.Application.Abstractions;

public interface ISalesReturnService
{
    Task<IReadOnlyList<SalesReturnListItemDto>> ListAsync(SalesReturnListQuery query,
        CancellationToken cancellationToken = default);

    Task<OrderCreatedDto> CreateAsync(CreateSalesReturnRequest request,
        CancellationToken cancellationToken = default);

    Task PatchStatusAsync(Guid id, PatchOrderStatusRequest request, CancellationToken cancellationToken = default);

    Task<SalesReturnDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesReturnCashbookRowDto>> ListCashbookEntriesAsync(Guid id,
        CancellationToken cancellationToken = default);
}
