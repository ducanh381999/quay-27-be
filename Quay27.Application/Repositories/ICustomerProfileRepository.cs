using Quay27.Application.CustomerProfiles;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerProfileRepository
{
    Task<IReadOnlyList<CustomerProfile>> ListAsync(string? search = null, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerProfile> Items, int TotalCount)> ListPagedAsync(CustomerProfileListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, CustomerProfileSalesAggregate>> GetSalesAggregatesByProfileIdsAsync(
        IReadOnlyList<Guid> profileIds,
        CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CustomerCodeExistsAsync(string customerCode, CancellationToken cancellationToken = default);

    /// <summary>True if a non-deleted profile exists with the same Phone1 (exact trim match).</summary>
    Task<bool> ExistsActiveByPhone1Async(string phone1, CancellationToken cancellationToken = default);

    Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default);
}
