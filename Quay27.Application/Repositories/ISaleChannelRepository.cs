using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ISaleChannelRepository
{
    Task<IReadOnlyList<SaleChannel>> ListActiveOrderedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SaleChannel>> ListAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(SaleChannel entity, CancellationToken cancellationToken = default);
}
