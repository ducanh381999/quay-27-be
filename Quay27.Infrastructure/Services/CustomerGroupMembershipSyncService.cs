using Quay27.Application.Abstractions;
using Quay27.Application.CustomerGroups;
using Quay27.Application.Repositories;

namespace Quay27.Infrastructure.Services;

public sealed class CustomerGroupMembershipSyncService : ICustomerGroupMembershipSyncService
{
    private readonly ICustomerGroupRepository _groups;
    private readonly ICustomerProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerGroupMembershipSyncService(
        ICustomerGroupRepository groups,
        ICustomerProfileRepository profiles,
        IUnitOfWork unitOfWork)
    {
        _groups = groups;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task SyncAsync(Guid customerGroupId, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetByIdAsync(customerGroupId, cancellationToken);
        if (group is null)
            return;

        var mode = (group.MembershipUpdateMode ?? "none").Trim().ToLowerInvariant();
        if (mode == "none")
            return;

        var conditions = CustomerGroupRulesJson.DeserializeConditions(group.RulesJson);
        var matching = await _profiles.FindActiveProfileIdsMatchingGroupConditionsAsync(
            conditions,
            group.CombineAllConditions,
            cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (mode == "replace")
            {
                await _profiles.ClearCustomerGroupFromProfilesNotInMatchingSetAsync(
                    group.Name,
                    matching,
                    cancellationToken);
            }

            await _profiles.AssignCustomerGroupToProfileIdsAsync(
                group.Name,
                matching,
                cancellationToken);
        }, cancellationToken);
    }
}
