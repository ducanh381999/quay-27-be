using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IDuplicateFlagRepository
{
    Task ReplaceFlagsForCustomersAsync(IReadOnlyList<Guid> customerIds, int duplicateGroupId, CancellationToken cancellationToken = default);
    Task ClearFlagsForCustomersAsync(IReadOnlyList<Guid> customerIds, CancellationToken cancellationToken = default);
}
