using Quay27.Application.CustomerGroups;
using Quay27.Application.CustomerProfiles;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerProfileRepository
{
    Task<IReadOnlyList<CustomerProfile>> ListAsync(string? search = null, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerProfileListPageRow> Items, int TotalCount)> ListPagedAsync(
        CustomerProfileListQuery query,
        CancellationToken cancellationToken = default);

    Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CustomerCodeExistsAsync(string customerCode, CancellationToken cancellationToken = default);

    /// <summary>True if a non-deleted profile exists with the same Phone1 (exact trim match).</summary>
    Task<bool> ExistsActiveByPhone1Async(string phone1, CancellationToken cancellationToken = default);

    /// <summary>True if a non-deleted profile exists with the same Email (exact trim match).</summary>
    Task<bool> ExistsActiveByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListDistinctCreatorsAsync(CancellationToken cancellationToken = default);

    Task<HashSet<Guid>> FindActiveProfileIdsMatchingGroupConditionsAsync(
        IReadOnlyList<CustomerGroupConditionDto> conditions,
        bool combineAll,
        CancellationToken cancellationToken = default);

    Task ClearCustomerGroupFromProfilesNotInMatchingSetAsync(
        string groupName,
        IReadOnlyCollection<Guid> matchingProfileIds,
        CancellationToken cancellationToken = default);

    Task AssignCustomerGroupToProfileIdsAsync(
        string groupName,
        IReadOnlyCollection<Guid> profileIds,
        CancellationToken cancellationToken = default);

    /// <summary>CRM display debt: manual override if set, else unpaid invoice total for profile.</summary>
    Task<decimal> GetDisplayDebtAsync(Guid customerProfileId, CancellationToken cancellationToken = default);
}
