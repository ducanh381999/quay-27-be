using Quay27.Application.Products;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(ProductQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> ListAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> ListByGroupIdsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
