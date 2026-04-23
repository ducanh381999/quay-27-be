using Quay27.Application.Abstractions;
using Quay27.Application.Products;
using Quay27.Application.Repositories;
using Quay27.Application.Services;
using Quay27.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Quay27.Products.Tests;

public class ProductServiceImageUrlTests
{
    private static readonly Guid ProductId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Theory]
    [InlineData("https://cdn.example.com/a.png", "https://cdn.example.com/a.png")]
    [InlineData("  https://cdn.example.com/a.png  ", "https://cdn.example.com/a.png")]
    [InlineData("http://cdn.example.com/a.png", "http://cdn.example.com/a.png")]
    public void NormalizeDisplayImageUrl_accepts_http_and_https_absolute(string input, string expected)
    {
        Assert.Equal(expected, ProductMappings.NormalizeDisplayImageUrl(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/relative/path.png")]
    [InlineData("ftp://host/file.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a url")]
    public void NormalizeDisplayImageUrl_rejects_invalid_or_unsafe(string? input)
    {
        Assert.Null(ProductMappings.NormalizeDisplayImageUrl(input));
    }

    [Fact]
    public async Task GetAsync_returns_null_ImageUrl_when_stored_value_is_not_display_safe()
    {
        var entity = MinimalProduct(ProductId, imageUrl: "javascript:evil()");
        var service = CreateService(entity);

        var dto = await service.GetAsync(ProductId, CancellationToken.None);

        Assert.Null(dto.ImageUrl);
    }

    [Fact]
    public async Task GetAsync_returns_canonical_https_ImageUrl_when_stored_value_is_valid()
    {
        var entity = MinimalProduct(ProductId, imageUrl: "https://cdn.example.com/x.png");
        var service = CreateService(entity);

        var dto = await service.GetAsync(ProductId, CancellationToken.None);

        Assert.Equal("https://cdn.example.com/x.png", dto.ImageUrl);
    }

    [Fact]
    public async Task ListAsync_applies_same_normalization_as_GetAsync()
    {
        var a = MinimalProduct(Guid.Parse("11111111-1111-1111-1111-111111111111"), "https://a.test/1.png");
        var b = MinimalProduct(Guid.Parse("22222222-2222-2222-2222-222222222222"), "ftp://bad");
        var service = CreateServiceForList([a, b]);

        var list = await service.ListAsync(new ProductQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(2, list.Items.Count);
        var byId = list.Items.ToDictionary(x => x.Id);
        Assert.Equal("https://a.test/1.png", byId[a.Id].ImageUrl);
        Assert.Null(byId[b.Id].ImageUrl);
    }

    [Fact]
    public async Task CreateAsync_rethrows_when_save_fails_after_image_operation()
    {
        var service = new ProductService(
            new SingleProductRepository(MinimalProduct(Guid.NewGuid(), null)),
            new NoopGroups(),
            new NoopPriceLists(),
            new NoopPriceListItems(),
            new FakeCurrentUser(isAuthenticated: true),
            new ThrowingUnitOfWork(),
            NullLogger<ProductService>.Instance);

        var request = new UpsertProductRequest
        {
            Name = "P",
            ItemType = "goods",
            CostPrice = 1,
            SalePrice = 1,
            Stock = 0,
            MinStock = 0,
            MaxStock = 0,
            WeightValue = 0,
            WeightUnit = "g",
            DirectSale = true,
            UploadedImageAssets = [new UploadedImageReferenceDto { AssetId = "a", Url = "https://cdn.test/x.png" }],
            Images = ["https://cdn.test/x.png"]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    private static Product MinimalProduct(Guid id, string? imageUrl) =>
        new()
        {
            Id = id,
            Code = "C",
            Name = "N",
            ItemType = "goods",
            SalePrice = 1,
            CostPrice = 1,
            Stock = 0,
            ImageUrl = imageUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            RowStatus = "active",
            DirectSale = true,
        };

    private static ProductService CreateService(Product entity)
    {
        var products = new SingleProductRepository(entity);
        return new ProductService(
            products,
            new NoopGroups(),
            new NoopPriceLists(),
            new NoopPriceListItems(),
            new FakeCurrentUser(isAuthenticated: true),
            new NoopUnitOfWork(),
            NullLogger<ProductService>.Instance);
    }

    private static ProductService CreateServiceForList(IReadOnlyList<Product> items)
    {
        var products = new ListProductRepository(items);
        return new ProductService(
            products,
            new NoopGroups(),
            new NoopPriceLists(),
            new NoopPriceListItems(),
            new FakeCurrentUser(isAuthenticated: true),
            new NoopUnitOfWork(),
            NullLogger<ProductService>.Instance);
    }

    private sealed class SingleProductRepository : IProductRepository
    {
        private readonly Product _entity;

        public SingleProductRepository(Product entity) => _entity = entity;

        public Task AddAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == _entity.Id ? _entity : null);

        public Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(null);

        public Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(ProductQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Product>)Array.Empty<Product>(), 0));

        public Task<IReadOnlyList<Product>> ListAllActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)Array.Empty<Product>());

        public Task<IReadOnlyList<Product>> ListByGroupIdsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)Array.Empty<Product>());
    }

    private sealed class ListProductRepository : IProductRepository
    {
        private readonly IReadOnlyList<Product> _items;

        public ListProductRepository(IReadOnlyList<Product> items) => _items = items;

        public Task AddAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(null);

        public Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(null);

        public Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(ProductQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Product>)_items.ToList(), _items.Count));

        public Task<IReadOnlyList<Product>> ListAllActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)Array.Empty<Product>());

        public Task<IReadOnlyList<Product>> ListByGroupIdsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)Array.Empty<Product>());
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

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("save failed");
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default) =>
            action();
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
    }

    private sealed class NoopGroups : IProductGroupRepository
    {
        public Task AddAsync(ProductGroup entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProductGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<ProductGroup?>(null);
        public Task<ProductGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ProductGroup?>(null);
        public Task<IReadOnlyList<ProductGroup>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<ProductGroup>)Array.Empty<ProductGroup>());
    }

    private sealed class NoopPriceLists : IPriceListRepository
    {
        public Task AddAsync(PriceList entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PriceList?>(null);
        public Task<PriceList?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PriceList?>(null);
        public Task<IReadOnlyList<PriceList>> ListAsync(string? search, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<PriceList>)Array.Empty<PriceList>());
        public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class NoopPriceListItems : IPriceListItemRepository
    {
        public Task AddRangeAsync(IReadOnlyList<PriceListItem> items, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PriceListItem?> GetTrackedAsync(Guid priceListId, Guid productId, CancellationToken cancellationToken = default) => Task.FromResult<PriceListItem?>(null);
        public Task<IReadOnlyList<PriceListItem>> ListByPriceListIdsAsync(IReadOnlyList<Guid> priceListIds, string? search, string? groupId, string? stock, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<PriceListItem>)Array.Empty<PriceListItem>());
    }

}
