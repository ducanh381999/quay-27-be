using Quay27.Application.Orders;

namespace Quay27.Application.Abstractions;

public interface ISalesReturnService
{
    Task<IReadOnlyList<SalesReturnListItemDto>> ListAsync(SalesReturnListQuery query,
        CancellationToken cancellationToken = default);

    Task<OrderCreatedDto> CreateAsync(CreateSalesReturnRequest request,
        CancellationToken cancellationToken = default);
}
