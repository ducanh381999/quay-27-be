using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerReceivableTransactionRepository
{
    Task<(IReadOnlyList<CustomerReceivableTransaction> Items, int TotalCount)> ListPagedAsync(
        Guid customerProfileId,
        int skip,
        int take,
        string? kind,
        CancellationToken cancellationToken = default);

    Task AddAsync(CustomerReceivableTransaction entity, CancellationToken cancellationToken = default);
}
