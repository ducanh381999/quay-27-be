using Quay27.Application.Orders;

namespace Quay27.Application.Abstractions;

public interface ISalesInvoiceService
{
    Task<IReadOnlyList<SalesInvoiceListItemDto>> ListAsync(SalesInvoiceListQuery query,
        CancellationToken cancellationToken = default);

    Task<OrderCreatedDto> CreateAsync(CreateSalesInvoiceRequest request,
        CancellationToken cancellationToken = default);
}
