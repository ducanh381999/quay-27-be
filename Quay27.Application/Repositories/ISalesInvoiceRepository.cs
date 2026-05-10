using Quay27.Application.Orders;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ISalesInvoiceRepository
{
    Task<IReadOnlyList<SalesInvoiceListItemDto>> ListAsync(SalesInvoiceListQuery query,
        CancellationToken cancellationToken = default);

    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SalesInvoice entity, CancellationToken cancellationToken = default);
}
