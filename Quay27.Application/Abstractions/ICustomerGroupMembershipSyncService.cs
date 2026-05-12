namespace Quay27.Application.Abstractions;

public interface ICustomerGroupMembershipSyncService
{
    /// <summary>Re-evaluates rules for the group and updates customer profile group assignment per stored membership mode.</summary>
    Task SyncAsync(Guid customerGroupId, CancellationToken cancellationToken = default);
}
