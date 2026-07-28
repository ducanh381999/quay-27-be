using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using Quay27.Application.Abstractions;
using Quay27.Application.Orders;
using Quay27.Application.Products;
using Quay27.Application.Repositories;
using Quay27.Application.Services;
using Quay27.Domain.Entities;

namespace Quay27.Products.Tests;

public class ProductImportServiceTests
{
    [Fact]
    public async Task ImportProductsExcelAsync_should_create_new_product()
    {
        var repo = new ImportProductRepository([]);
        var groups = new ImportProductGroupRepository();
        var service = CreateService(repo, groups);

        var result = await service.ImportProductsExcelAsync(new ImportProductsExcelRequest
        {
            FileBytes = BuildWorkbookBytes([
                new object?[] { "SP001", "Ao so mi", "8930001", "Thoi trang", "Brand A", 100000m, 150000m, 12, 2, 50, "Ke 1", 500m, "g", "Mo ta moi", "Có", "goods" }
            ]),
            FileName = "MauFileSanPham.xlsx",
            DuplicateCodeConflictAction = ProductImportConflictAction.Error,
            DuplicateBarcodeConflictAction = ProductImportConflictAction.Error,
            UpdateStock = false,
            UpdateCostPrice = false,
            UpdateDescription = false
        }, CancellationToken.None);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Empty(result.Errors);
        var created = Assert.Single(repo.AddedProducts);
        Assert.Equal("SP001", created.Code);
        Assert.Equal("Ao so mi", created.Name);
        Assert.Equal("8930001", created.Barcode);
        Assert.Equal(12, created.Stock);
        Assert.Equal(100000m, created.CostPrice);
        Assert.Equal("Mo ta moi", created.Description);
        Assert.Equal("Thoi trang", groups.AddedGroups.Single().Name);
    }

    [Fact]
    public async Task ImportProductsExcelAsync_should_preserve_optional_fields_when_flags_are_off()
    {
        var existing = BuildProduct(
            id: Guid.NewGuid(),
            code: "SP001",
            name: "Ao cu",
            barcode: "8930001",
            costPrice: 50000m,
            salePrice: 120000m,
            stock: 7,
            description: "Mo ta cu");
        var repo = new ImportProductRepository([existing]);
        var service = CreateService(repo, new ImportProductGroupRepository());

        var result = await service.ImportProductsExcelAsync(new ImportProductsExcelRequest
        {
            FileBytes = BuildWorkbookBytes([
                new object?[] { "SP001", "Ao cu", "8930001", null, null, 999999m, 220000m, 99, 1, 9, null, null, null, "Mo ta moi", "Không", "goods" }
            ]),
            FileName = "MauFileSanPham.xlsx",
            DuplicateCodeConflictAction = ProductImportConflictAction.Error,
            DuplicateBarcodeConflictAction = ProductImportConflictAction.Error,
            UpdateStock = false,
            UpdateCostPrice = false,
            UpdateDescription = false
        }, CancellationToken.None);

        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(7, existing.Stock);
        Assert.Equal(50000m, existing.CostPrice);
        Assert.Equal("Mo ta cu", existing.Description);
        Assert.Equal(220000m, existing.SalePrice);
    }

    [Fact]
    public async Task ImportProductsExcelAsync_should_fail_when_duplicate_code_has_different_name_and_action_is_error()
    {
        var existing = BuildProduct(Guid.NewGuid(), "SP001", "Ao cu", "8930001", 50000m, 120000m, 7, "Mo ta cu");
        var repo = new ImportProductRepository([existing]);
        var service = CreateService(repo, new ImportProductGroupRepository());

        var result = await service.ImportProductsExcelAsync(new ImportProductsExcelRequest
        {
            FileBytes = BuildWorkbookBytes([
                new object?[] { "SP001", "Ten moi", "8930001", null, null, 50000m, 120000m, 7, 0, 0, null, null, null, null, "Có", "goods" }
            ]),
            FileName = "MauFileSanPham.xlsx",
            DuplicateCodeConflictAction = ProductImportConflictAction.Error,
            DuplicateBarcodeConflictAction = ProductImportConflictAction.Error
        }, CancellationToken.None);

        Assert.Equal(1, result.FailedCount);
        Assert.Contains(result.Errors, x => x.Contains("tên hiện tại khác tên"));
        Assert.Equal("Ao cu", existing.Name);
    }

    [Fact]
    public async Task ImportProductsExcelAsync_should_replace_name_when_duplicate_code_action_is_replace()
    {
        var existing = BuildProduct(Guid.NewGuid(), "SP001", "Ao cu", "8930001", 50000m, 120000m, 7, "Mo ta cu");
        var repo = new ImportProductRepository([existing]);
        var service = CreateService(repo, new ImportProductGroupRepository());

        var result = await service.ImportProductsExcelAsync(new ImportProductsExcelRequest
        {
            FileBytes = BuildWorkbookBytes([
                new object?[] { "SP001", "Ten moi", "8930001", null, null, 50000m, 120000m, 7, 0, 0, null, null, null, null, "Có", "goods" }
            ]),
            FileName = "MauFileSanPham.xlsx",
            DuplicateCodeConflictAction = ProductImportConflictAction.Replace,
            DuplicateBarcodeConflictAction = ProductImportConflictAction.Error
        }, CancellationToken.None);

        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal("Ten moi", existing.Name);
    }

    [Fact]
    public async Task ImportProductsExcelAsync_should_replace_code_when_barcode_conflict_action_is_replace()
    {
        var existing = BuildProduct(Guid.NewGuid(), "OLD001", "Ao cu", "8930001", 50000m, 120000m, 7, "Mo ta cu");
        var repo = new ImportProductRepository([existing]);
        var service = CreateService(repo, new ImportProductGroupRepository());

        var result = await service.ImportProductsExcelAsync(new ImportProductsExcelRequest
        {
            FileBytes = BuildWorkbookBytes([
                new object?[] { "NEW001", "Ao cu", "8930001", null, null, 50000m, 120000m, 7, 0, 0, null, null, null, null, "Có", "goods" }
            ]),
            FileName = "MauFileSanPham.xlsx",
            DuplicateCodeConflictAction = ProductImportConflictAction.Error,
            DuplicateBarcodeConflictAction = ProductImportConflictAction.Replace
        }, CancellationToken.None);

        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal("NEW001", existing.Code);
    }

    private static ProductService CreateService(ImportProductRepository repo, ImportProductGroupRepository groups)
    {
        return new ProductService(
            repo,
            groups,
            new NoopPriceLists(),
            new NoopPriceListItems(),
            new NoopPurchaseOrders(),
            new FakeCurrentUser(),
            new NoopUnitOfWork(),
            NullLogger<ProductService>.Instance);
    }

    private static Product BuildProduct(Guid id, string code, string name, string barcode, decimal costPrice, decimal salePrice, int stock, string description)
    {
        return new Product
        {
            Id = id,
            Code = code,
            Name = name,
            Barcode = barcode,
            CostPrice = costPrice,
            SalePrice = salePrice,
            Stock = stock,
            Description = description,
            ItemType = "goods",
            RowStatus = "active",
            DirectSale = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static byte[] BuildWorkbookBytes(IReadOnlyList<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");
        string[] headers =
        [
            "Mã hàng", "Tên hàng", "Mã vạch", "Nhóm hàng", "Thương hiệu", "Giá vốn", "Giá bán",
            "Tồn kho", "Tồn tối thiểu", "Tồn tối đa", "Vị trí", "Khối lượng", "Đơn vị khối lượng",
            "Mô tả", "Bán trực tiếp", "Loại hàng"
        ];

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
        public IReadOnlyList<string> Roles => ["product_manager"];
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

    private sealed class ImportProductRepository(IReadOnlyList<Product> items) : IProductRepository
    {
        private readonly List<Product> _items = items.ToList();
        public List<Product> AddedProducts { get; } = [];

        public Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(ProductQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Product>)_items.ToList(), _items.Count));

        public Task<IReadOnlyList<Product>> ListAllActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)_items.Where(x => !x.IsDeleted && string.Equals(x.RowStatus, "active", StringComparison.OrdinalIgnoreCase)).ToList());

        public Task<IReadOnlyList<Product>> ListByGroupIdsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)Array.Empty<Product>());

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(x => x.Id != excludeId && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(x => x.Id != excludeId && string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            _items.Add(product);
            AddedProducts.Add(product);
            return Task.CompletedTask;
        }
    }

    private sealed class ImportProductGroupRepository : IProductGroupRepository
    {
        private readonly Dictionary<string, ProductGroup> _groups = new(StringComparer.OrdinalIgnoreCase);
        public List<ProductGroup> AddedGroups { get; } = [];

        public Task AddAsync(ProductGroup entity, CancellationToken cancellationToken = default)
        {
            _groups[entity.Name] = entity;
            AddedGroups.Add(entity);
            return Task.CompletedTask;
        }

        public Task<ProductGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(_groups.GetValueOrDefault(name));

        public Task<ProductGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_groups.Values.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<ProductGroup>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<ProductGroup>)_groups.Values.ToList());
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
        public Task<IReadOnlyList<PriceListItem>> ListByPriceListIdsAsync(IReadOnlyList<Guid> priceListIds, string? search, string? groupId, string? stock, IReadOnlyList<Guid>? filterGroupIds, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<PriceListItem>)Array.Empty<PriceListItem>());
        public Task<IReadOnlyDictionary<Guid, decimal>> GetPricesByProductIdsAsync(Guid priceListId, IReadOnlyList<Guid> productIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
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
