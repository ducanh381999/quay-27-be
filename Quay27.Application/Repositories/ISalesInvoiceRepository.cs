using Quay27.Application.Orders;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ISalesInvoiceRepository
{
    Task<IReadOnlyList<SalesInvoiceListItemDto>> ListAsync(SalesInvoiceListQuery query,
        CancellationToken cancellationToken = default);

    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SalesInvoice entity, CancellationToken cancellationToken = default);

    Task<SalesInvoice?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SalesInvoice?> GetByIdNoTrackingAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SalesInvoiceDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesInvoiceCashbookRowDto>> ListCashbookEntriesAsync(Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesInvoiceReturnRowDto>> ListReturnsAsync(Guid invoiceId,
        CancellationToken cancellationToken = default);
}
