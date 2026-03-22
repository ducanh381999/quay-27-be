using Quay27.Application.Users;

namespace Quay27.Application.Abstractions;

public interface ICustomerColumnPermissionService
{
    Task<CustomerColumnPermissionsResponse> GetForCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<CustomerColumnPermissionsResponse> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ReplaceForUserAsync(Guid userId, IReadOnlyList<CustomerColumnPermissionInput> items,
        CancellationToken cancellationToken = default);
}
