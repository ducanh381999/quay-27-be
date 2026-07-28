using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using Quay27.Application.Abstractions;
using Quay27.Application.Orders;
using Quay27.Application.Products;
using Quay27.Application.Repositories;
using Quay27.Application.Services;
using Quay27.Domain.Entities;

namespace Quay27.Products.Tests;

public class ProductExportServiceTests
{
    [Fact]
    public async Task ExportProductsExcelAsync_should_return_selected_columns_in_requested_order()
    {
        var rootGroup = new ProductGroup { Id = Guid.NewGuid(), Name = "Me", CreatedDate = DateTime.UtcNow };
        var childGroup = new ProductGroup
        {
            Id = Guid.NewGuid(),
            Name = "Con",
            ParentId = rootGroup.Id,
            CreatedDate = DateTime.UtcNow
        };

        var repo = new ExportProductRepository([
            new Product
            {
                Id = Guid.NewGuid(),
                Code = "SP001",
                Name = "Ao so mi",
                ItemType = "goods",
                SalePrice = 150000m,
                CostPrice = 100000m,
                Stock = 12,
                Barcode = "8930001",
                GroupId = childGroup.Id,
                Group = childGroup,
                Brand = "Brand A",
                DirectSale = true,
                RowStatus = "active",
                ImageUrl = "https://cdn.test/a.png",
                InvoiceNoteTemplate = "Ghi chu",
                CreatedAt = new DateTimeOffset(2026, 07, 28, 10, 30, 00, TimeSpan.Zero)
            }
        ]);
        var service = CreateService(repo, new ExportProductGroupRepository([rootGroup, childGroup]));

        var bytes = await service.ExportProductsExcelAsync(new ExportProductsExcelRequest
        {
            Columns =
            [
                new ExportProductsExcelColumn { Key = "code", HeaderName = "Mã hàng" },
                new ExportProductsExcelColumn { Key = "groupName3Level", HeaderName = "Nhóm hàng(3 Cấp)" },
                new ExportProductsExcelColumn { Key = "imageUrls", HeaderName = "Hình ảnh (url1,url2...)" },
                new ExportProductsExcelColumn { Key = "invoiceNoteTemplate", HeaderName = "Mẫu ghi chú" }
            ]
        }, CancellationToken.None);

        Assert.NotNull(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes!));
        var ws = workbook.Worksheet(1);

        Assert.Equal("Mã hàng", ws.Cell(1, 1).GetString());
        Assert.Equal("Nhóm hàng(3 Cấp)", ws.Cell(1, 2).GetString());
        Assert.Equal("Hình ảnh (url1,url2...)", ws.Cell(1, 3).GetString());
        Assert.Equal("Mẫu ghi chú", ws.Cell(1, 4).GetString());
        Assert.Equal("SP001", ws.Cell(2, 1).GetString());
        Assert.Equal("Me > Con", ws.Cell(2, 2).GetString());
        Assert.Equal("https://cdn.test/a.png", ws.Cell(2, 3).GetString());
        Assert.Equal("Ghi chu", ws.Cell(2, 4).GetString());
    }

    [Fact]
    public async Task ExportProductsExcelAsync_should_export_all_rows_matching_filter_across_pages()
    {
        var items = Enumerable.Range(1, 520)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Code = $"SP{i:000}",
                Name = $"Ao {i:000}",
                ItemType = "goods",
                SalePrice = i,
                CostPrice = i,
                Stock = i,
                DirectSale = true,
                RowStatus = "active",
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();
        var repo = new ExportProductRepository(items);
        var service = CreateService(repo, new ExportProductGroupRepository([]));

        var bytes = await service.ExportProductsExcelAsync(new ExportProductsExcelRequest
        {
            Search = "Ao",
            Columns =
            [
                new ExportProductsExcelColumn { Key = "code", HeaderName = "Mã hàng" }
            ]
        }, CancellationToken.None);

        Assert.NotNull(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes!));
        var ws = workbook.Worksheet(1);
        Assert.Equal("SP001", ws.Cell(2, 1).GetString());
        Assert.Equal("SP520", ws.Cell(521, 1).GetString());
    }

    private static ProductService CreateService(ExportProductRepository repo, ExportProductGroupRepository groups)
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

    private sealed class ExportProductRepository(IReadOnlyList<Product> items) : IProductRepository
    {
        private readonly List<Product> _items = items.ToList();

        public Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(ProductQuery query, CancellationToken cancellationToken = default)
        {
            IEnumerable<Product> filtered = _items.Where(x => !x.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                filtered = filtered.Where(x => x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var total = filtered.Count();
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 100 : query.PageSize;
            var pageItems = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Task.FromResult(((IReadOnlyList<Product>)pageItems, total));
        }

        public Task<IReadOnlyList<Product>> ListAllActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Product>)_items.Where(x => !x.IsDeleted && x.RowStatus == "active").ToList());

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
            return Task.CompletedTask;
        }
    }

    private sealed class ExportProductGroupRepository(IReadOnlyList<ProductGroup> groups) : IProductGroupRepository
    {
        private readonly List<ProductGroup> _groups = groups.ToList();

        public Task AddAsync(ProductGroup entity, CancellationToken cancellationToken = default)
        {
            _groups.Add(entity);
            return Task.CompletedTask;
        }

        public Task<ProductGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(_groups.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<ProductGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_groups.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<ProductGroup>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<ProductGroup>)_groups.ToList());
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
