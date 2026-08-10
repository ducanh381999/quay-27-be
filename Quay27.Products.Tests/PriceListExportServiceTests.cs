using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using Quay27.Application.Abstractions;
using Quay27.Application.Orders;
using Quay27.Application.Products;
using Quay27.Application.Repositories;
using Quay27.Application.Services;
using Quay27.Application.Validators;
using Quay27.Domain.Entities;

namespace Quay27.Products.Tests;

public class PriceListExportServiceTests
{
    private readonly PriceListItemsQueryValidator _validator = new();

    [Fact]
    public void Should_pass_when_export_query_has_required_price_list_ids()
    {
        var result = _validator.Validate(new PriceListItemsQuery
        {
            PriceListIds = [Guid.NewGuid()]
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExportPriceListAsync_should_create_one_price_column_per_selected_price_list()
    {
        var priceListA = new PriceList { Id = Guid.NewGuid(), Name = "Bảng giá lẻ" };
        var priceListB = new PriceList { Id = Guid.NewGuid(), Name = "Bảng giá sỉ" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "SP001",
            Name = "Áo sơ mi",
            CostPrice = 100000m,
            RowStatus = "active",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var service = CreateService(
            products: [product],
            priceLists: [priceListA, priceListB],
            items:
            [
                new PriceListItem { PriceListId = priceListA.Id, ProductId = product.Id, Product = product, Price = 120000m, CreatedAt = DateTimeOffset.UtcNow },
                new PriceListItem { PriceListId = priceListB.Id, ProductId = product.Id, Product = product, Price = 90000m, CreatedAt = DateTimeOffset.UtcNow }
            ]);

        var bytes = await service.ExportPriceListAsync(new PriceListItemsQuery
        {
            PriceListIds = [priceListA.Id, priceListB.Id]
        }, CancellationToken.None);

        Assert.NotNull(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes!));
        var ws = workbook.Worksheet(1);
        Assert.Equal("Bảng giá lẻ", ws.Cell(1, 5).GetString());
        Assert.Equal("Bảng giá sỉ", ws.Cell(1, 6).GetString());
        Assert.Equal("120000", ws.Cell(2, 5).GetString());
        Assert.Equal("90000", ws.Cell(2, 6).GetString());
    }

    [Fact]
    public async Task ImportPriceListAsync_should_support_matrix_template_for_selected_price_lists()
    {
        var priceListA = new PriceList { Id = Guid.NewGuid(), Name = "Bảng giá lẻ" };
        var priceListB = new PriceList { Id = Guid.NewGuid(), Name = "Bảng giá sỉ" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "SP001",
            Name = "Áo sơ mi",
            RowStatus = "active",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var itemRepo = new FakePriceListItemRepository([]);
        var service = CreateService(
            products: [product],
            priceLists: [priceListA, priceListB],
            items: itemRepo.Items,
            itemRepositoryOverride: itemRepo);

        var result = await service.ImportPriceListAsync(new PriceListImportRequest
        {
            FileBytes = BuildMatrixWorkbookBytes([
                new object?[] { "SP001", "Áo sơ mi", 120000m, 90000m }
            ]),
            FileName = "MauFileBangGia.xlsx",
            SelectedPriceListIds = [priceListA.Id, priceListB.Id]
        }, CancellationToken.None);

        Assert.Equal(1, result.SuccessfulRows);
        Assert.Equal(0, result.FailedRows);
        Assert.Equal(2, itemRepo.AddedItems.Count);
        Assert.Contains(itemRepo.AddedItems, x => x.PriceListId == priceListA.Id && x.Price == 120000m);
        Assert.Contains(itemRepo.AddedItems, x => x.PriceListId == priceListB.Id && x.Price == 90000m);
    }

    private static ProductService CreateService(
        IReadOnlyList<Product> products,
        IReadOnlyList<PriceList> priceLists,
        IReadOnlyList<PriceListItem> items,
        FakePriceListItemRepository? itemRepositoryOverride = null)
    {
        return new ProductService(
            new FakeProductRepository(products),
            new FakeProductGroupRepository(),
            new FakePriceListRepository(priceLists),
            itemRepositoryOverride ?? new FakePriceListItemRepository(items),
            new NoopPurchaseOrders(),
            new FakeCurrentUser(),
            new NoopUnitOfWork(),
            NullLogger<ProductService>.Instance);
    }

    private static byte[] BuildMatrixWorkbookBytes(IReadOnlyList<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("PriceImport");
        string[] headers = ["Mã hàng", "Tên hàng", "Tên bảng giá 1", "Tên bảng giá 2"];
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c]?.ToString() ?? string.Empty;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.NewGuid();
        public string Username => "test";
        public bool IsAuthenticated => true;
        public IReadOnlyList<string> Roles => ["price_settings_manager"];
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

    private sealed class FakeProductRepository(IReadOnlyList<Product> products) : IProductRepository
    {
        private readonly List<Product> _products = products.ToList();

        public Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(ProductQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Product>)_products.ToList(), _products.Count));

        public Task<IReadOnlyList<Product>> ListAllActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)_products.ToList());

        public Task<IReadOnlyList<Product>> ListByGroupIdsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)Array.Empty<Product>());

        public Task<IReadOnlyDictionary<Guid, int>> CountActiveByGroupAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyDictionary<Guid, int>)new Dictionary<Guid, int>());

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.FirstOrDefault(x => x.Id == id));

        public Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.FirstOrDefault(x => x.Id == id));

        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeProductGroupRepository : IProductGroupRepository
    {
        public Task AddAsync(ProductGroup entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProductGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<ProductGroup?>(null);
        public Task<ProductGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ProductGroup?>(null);
        public Task<IReadOnlyList<ProductGroup>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<ProductGroup>)Array.Empty<ProductGroup>());
    }

    private sealed class FakePriceListRepository(IReadOnlyList<PriceList> priceLists) : IPriceListRepository
    {
        private readonly List<PriceList> _priceLists = priceLists.ToList();

        public Task<IReadOnlyList<PriceList>> ListAsync(string? search, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<PriceList>)_priceLists.ToList());

        public Task<PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_priceLists.FirstOrDefault(x => x.Id == id));

        public Task<PriceList?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_priceLists.FirstOrDefault(x => x.Id == id));

        public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(PriceList entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePriceListItemRepository(IReadOnlyList<PriceListItem> items) : IPriceListItemRepository
    {
        private readonly List<PriceListItem> _items = items.ToList();
        public IReadOnlyList<PriceListItem> Items => _items;
        public List<PriceListItem> AddedItems { get; } = [];

        public Task<IReadOnlyList<PriceListItem>> ListByPriceListIdsAsync(
            IReadOnlyList<Guid> priceListIds,
            string? search,
            string? groupId,
            string? stock,
            IReadOnlyList<Guid>? filterGroupIds,
            CancellationToken cancellationToken = default)
        {
            var result = _items.Where(x => priceListIds.Contains(x.PriceListId)).ToList();
            return Task.FromResult((IReadOnlyList<PriceListItem>)result);
        }

        public Task<PriceListItem?> GetTrackedAsync(Guid priceListId, Guid productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.PriceListId == priceListId && x.ProductId == productId));

        public Task AddRangeAsync(IReadOnlyList<PriceListItem> items, CancellationToken cancellationToken = default)
        {
            _items.AddRange(items);
            AddedItems.AddRange(items);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<Guid, decimal>> GetPricesByProductIdsAsync(Guid priceListId, IReadOnlyList<Guid> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
    }

    private sealed class NoopPurchaseOrders : IPurchaseOrderRepository
    {
        public Task AddAsync(PurchaseOrder entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default) => Task.FromResult("DH000001");
        public Task<PurchaseOrder?> GetByIdNoTrackingAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrder?>(null);
        public Task<PurchaseOrder?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrder?>(null);
        public Task<IReadOnlyList<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<PurchaseOrderListItemDto>)Array.Empty<PurchaseOrderListItemDto>());
        public Task<int> SumReservedQuantityForProductInOpenOrdersAsync(Guid productId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<PurchaseOrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrderDetailDto?>(null);
        public Task<IReadOnlyList<PurchaseOrderLinkedInvoiceDto>> ListLinkedInvoicesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<PurchaseOrderLinkedInvoiceDto>)Array.Empty<PurchaseOrderLinkedInvoiceDto>());
        public Task<IReadOnlyList<PurchaseOrderCashbookRowDto>> ListCashbookEntriesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<PurchaseOrderCashbookRowDto>)Array.Empty<PurchaseOrderCashbookRowDto>());
    }
}
