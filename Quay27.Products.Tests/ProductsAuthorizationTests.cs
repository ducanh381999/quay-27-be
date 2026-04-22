using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;
using Quay27.Application.Services;

namespace Quay27.Products.Tests;

public class ProductsAuthorizationTests
{
    [Fact]
    public async Task Should_require_authenticated_user_for_import_export_actions()
    {
        var service = new ProductService(
            new NoopProducts(),
            new NoopGroups(),
            new NoopPriceLists(),
            new NoopPriceListItems(),
            new FakeCurrentUser(isAuthenticated: false),
            new NoopUnitOfWork());

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ExportPriceListAsync(new()));
    }

    private sealed class FakeCurrentUser(bool isAuthenticated) : ICurrentUser
    {
        public Guid? UserId => isAuthenticated ? Guid.NewGuid() : null;
        public string Username => "test";
        public bool IsAuthenticated => isAuthenticated;
        public IReadOnlyList<string> Roles => Array.Empty<string>();
        public bool IsAdmin => false;
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default) => action();
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
    }

    private sealed class NoopProducts : IProductRepository
    {
        public Task AddAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Domain.Entities.Product?>(null);
        public Task<Domain.Entities.Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Domain.Entities.Product?>(null);
        public Task<(IReadOnlyList<Domain.Entities.Product> Items, int Total)> ListAsync(Application.Products.ProductQuery query, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<Domain.Entities.Product>)Array.Empty<Domain.Entities.Product>(), 0));
        public Task<IReadOnlyList<Domain.Entities.Product>> ListAllActiveAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Domain.Entities.Product>)Array.Empty<Domain.Entities.Product>());
        public Task<IReadOnlyList<Domain.Entities.Product>> ListByGroupIdsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Domain.Entities.Product>)Array.Empty<Domain.Entities.Product>());
    }

    private sealed class NoopGroups : IProductGroupRepository
    {
        public Task AddAsync(Domain.Entities.ProductGroup entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Domain.Entities.ProductGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<Domain.Entities.ProductGroup?>(null);
        public Task<Domain.Entities.ProductGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Domain.Entities.ProductGroup?>(null);
        public Task<IReadOnlyList<Domain.Entities.ProductGroup>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Domain.Entities.ProductGroup>)Array.Empty<Domain.Entities.ProductGroup>());
    }

    private sealed class NoopPriceLists : IPriceListRepository
    {
        public Task AddAsync(Domain.Entities.PriceList entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Domain.Entities.PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Domain.Entities.PriceList?>(null);
        public Task<Domain.Entities.PriceList?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Domain.Entities.PriceList?>(null);
        public Task<IReadOnlyList<Domain.Entities.PriceList>> ListAsync(string? search, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Domain.Entities.PriceList>)Array.Empty<Domain.Entities.PriceList>());
        public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class NoopPriceListItems : IPriceListItemRepository
    {
        public Task AddRangeAsync(IReadOnlyList<Domain.Entities.PriceListItem> items, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Domain.Entities.PriceListItem?> GetTrackedAsync(Guid priceListId, Guid productId, CancellationToken cancellationToken = default) => Task.FromResult<Domain.Entities.PriceListItem?>(null);
        public Task<IReadOnlyList<Domain.Entities.PriceListItem>> ListByPriceListIdsAsync(IReadOnlyList<Guid> priceListIds, string? search, string? groupId, string? stock, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Domain.Entities.PriceListItem>)Array.Empty<Domain.Entities.PriceListItem>());
    }
}
