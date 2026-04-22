using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IPriceListRepository
{
    Task<IReadOnlyList<PriceList>> ListAsync(string? search, CancellationToken cancellationToken = default);
    Task<PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PriceList?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(PriceList entity, CancellationToken cancellationToken = default);
}
