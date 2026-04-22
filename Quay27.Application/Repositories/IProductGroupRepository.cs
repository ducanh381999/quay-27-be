using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IProductGroupRepository
{
    Task<IReadOnlyList<ProductGroup>> ListAsync(CancellationToken cancellationToken = default);
    Task<ProductGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<ProductGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ProductGroup group, CancellationToken cancellationToken = default);
}
