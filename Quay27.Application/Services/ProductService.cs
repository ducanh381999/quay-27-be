using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Products;
using Quay27.Application.Repositories;
using ClosedXML.Excel;
using Quay27.Domain.Entities;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Quay27.Application.Validators;

namespace Quay27.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IProductGroupRepository _groups;
    private readonly IPriceListRepository _priceLists;
    private readonly IPriceListItemRepository _priceListItems;
    private readonly IPurchaseOrderRepository _purchaseOrders;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository products,
        IProductGroupRepository groups,
        IPriceListRepository priceLists,
        IPriceListItemRepository priceListItems,
        IPurchaseOrderRepository purchaseOrders,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<ProductService> logger)
    {
        _products = products;
        _groups = groups;
        _priceLists = priceLists;
        _priceListItems = priceListItems;
        _purchaseOrders = purchaseOrders;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProductListResponse> ListAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (query.GroupIds is { Count: > 0 })
        {
            var parsed = query.GroupIds.Where(g => g != Guid.Empty).Distinct().ToList();
            if (parsed.Count > 0)
            {
                var allGroups = await _groups.ListAsync(cancellationToken);
                query = new ProductQuery
                {
                    Search = query.Search,
                    GroupId = null,
                    GroupIds = ExpandDescendantGroupIds(allGroups, parsed),
                    Stock = query.Stock,
                    DirectSale = query.DirectSale,
                    Status = query.Status,
                    CreatedFrom = query.CreatedFrom,
                    CreatedTo = query.CreatedTo,
                    ExpectedFrom = query.ExpectedFrom,
                    ExpectedTo = query.ExpectedTo,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    PriceListId = query.PriceListId
                };
            }
        }

        var (items, total) = await _products.ListAsync(query, cancellationToken);
        IReadOnlyDictionary<Guid, decimal>? listPrices = null;
        if (query.PriceListId is { } priceListId)
        {
            var pl = await _priceLists.GetByIdAsync(priceListId, cancellationToken);
            if (pl is { IsDeleted: false } &&
                string.Equals(pl.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                var ids = items.Select(i => i.Id).ToList();
                if (ids.Count > 0)
                {
                    listPrices =
                        await _priceListItems.GetPricesByProductIdsAsync(priceListId, ids, cancellationToken);
                }
            }
        }

        return new ProductListResponse
        {
            Items = items
                .Select(x => Map(
                    x,
                    null,
                    listPrices != null && listPrices.TryGetValue(x.Id, out var lp) ? lp : null))
                .ToList(),
            Total = total,
        };
    }

    public async Task<ProductListItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _products.GetByIdAsync(id, cancellationToken);
        if (item is null) throw new NotFoundException("Product not found.");
        return Map(item);
    }

    public async Task<ProductOrderEntryStockResponse> GetOrderEntryStockAsync(Guid productId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _products.GetByIdAsync(productId, cancellationToken);
        if (item is null) throw new NotFoundException("Product not found.");

        var reserved = await _purchaseOrders.SumReservedQuantityForProductInOpenOrdersAsync(productId, cancellationToken);
        var stock = item.Stock;
        var customerOrders = reserved;
        var available = Math.Max(0, stock - customerOrders);

        var totalRow = new ProductOrderEntryStockRowDto
        {
            Name = "Tổng cộng",
            Stock = stock,
            CustomerOrders = customerOrders,
            AvailableToSell = available,
        };

        // Placeholder second row until multi-branch inventory exists (same numbers as total).
        var branchRow = new ProductOrderEntryStockRowDto
        {
            Name = "Chi nhánh trung tâm",
            Stock = stock,
            CustomerOrders = customerOrders,
            AvailableToSell = available,
        };

        return new ProductOrderEntryStockResponse
        {
            ProductId = item.Id,
            ProductName = item.Name,
            Rows = new[] { totalRow, branchRow },
        };
    }

    public async Task<ProductListItemDto> CreateAsync(UpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var hasImageOperation = request.UploadedImageAssets.Count > 0 || request.Images.Count > 0;
        _logger.LogInformation(
            "product_save_attempt action=create user={UserId} hasImageOperation={HasImageOperation}",
            _currentUser.UserId,
            hasImageOperation);
        var code = await ResolveCodeAsync(request.Code, null, cancellationToken);
        await EnsureUniqueBarcodeAsync(request.Barcode, null, cancellationToken);
        var group = await ResolveGroupAsync(request.GroupName, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            ItemType = request.ItemType,
            Barcode = NormalizeNull(request.Barcode),
            GroupId = group?.Id,
            Brand = NormalizeNull(request.Brand),
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            Stock = request.Stock,
            MinStock = request.MinStock,
            MaxStock = request.MaxStock,
            Location = NormalizeNull(request.Location),
            RowStatus = "active",
            DirectSale = request.DirectSale,
            Description = NormalizeNull(request.Description),
            DescriptionRichText = NormalizeNull(request.DescriptionRichText),
            InvoiceNoteTemplate = NormalizeNull(request.InvoiceNoteTemplate),
            WeightKg = ToWeightKg(request.WeightValue, request.WeightUnit),
            ImageUrl = request.UploadedImageAssets.FirstOrDefault()?.Url ?? request.Images.FirstOrDefault(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _products.AddAsync(entity, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "product_save_failure action=create user={UserId} hasImageOperation={HasImageOperation} cleanup_pending={CleanupPending}",
                _currentUser.UserId,
                hasImageOperation,
                hasImageOperation);
            throw;
        }

        _logger.LogInformation(
            "product_save_success action=create productId={ProductId} user={UserId} hasImageOperation={HasImageOperation}",
            entity.Id,
            _currentUser.UserId,
            hasImageOperation);
        return Map(entity, group);
    }

    public async Task<ProductListItemDto> UpdateAsync(Guid id, UpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var hasImageOperation = request.UploadedImageAssets.Count > 0 || request.Images.Count > 0;
        _logger.LogInformation(
            "product_save_attempt action=update productId={ProductId} user={UserId} hasImageOperation={HasImageOperation}",
            id,
            _currentUser.UserId,
            hasImageOperation);
        var item = await _products.GetTrackedByIdAsync(id, cancellationToken);
        if (item is null) throw new NotFoundException("Product not found.");
        var code = await ResolveCodeAsync(request.Code, id, cancellationToken);
        await EnsureUniqueBarcodeAsync(request.Barcode, id, cancellationToken);
        var group = await ResolveGroupAsync(request.GroupName, cancellationToken);

        item.Code = code;
        item.Name = request.Name.Trim();
        item.ItemType = request.ItemType;
        item.Barcode = NormalizeNull(request.Barcode);
        item.GroupId = group?.Id;
        item.Brand = NormalizeNull(request.Brand);
        item.CostPrice = request.CostPrice;
        item.SalePrice = request.SalePrice;
        item.Stock = request.Stock;
        item.MinStock = request.MinStock;
        item.MaxStock = request.MaxStock;
        item.Location = NormalizeNull(request.Location);
        item.DirectSale = request.DirectSale;
        item.Description = NormalizeNull(request.Description);
        item.DescriptionRichText = NormalizeNull(request.DescriptionRichText);
        item.InvoiceNoteTemplate = NormalizeNull(request.InvoiceNoteTemplate);
        item.WeightKg = ToWeightKg(request.WeightValue, request.WeightUnit);
        item.ImageUrl = request.UploadedImageAssets.FirstOrDefault()?.Url ?? request.Images.FirstOrDefault();
        item.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "product_save_failure action=update productId={ProductId} user={UserId} hasImageOperation={HasImageOperation} cleanup_pending={CleanupPending}",
                id,
                _currentUser.UserId,
                hasImageOperation,
                hasImageOperation);
            throw;
        }

        _logger.LogInformation(
            "product_save_success action=update productId={ProductId} user={UserId} hasImageOperation={HasImageOperation}",
            id,
            _currentUser.UserId,
            hasImageOperation);
        return Map(item, group);
    }

    public async Task<ProductListItemDto> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var src = await _products.GetByIdAsync(id, cancellationToken);
        if (src is null) throw new NotFoundException("Product not found.");

        var code = await ResolveCodeAsync(null, null, cancellationToken);
        var copy = new Product
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = $"{src.Name} (Copy)",
            ItemType = src.ItemType,
            SalePrice = src.SalePrice,
            CostPrice = src.CostPrice,
            Stock = src.Stock,
            Barcode = null,
            GroupId = src.GroupId,
            Brand = src.Brand,
            Location = src.Location,
            MinStock = src.MinStock,
            MaxStock = src.MaxStock,
            RowStatus = src.RowStatus,
            DirectSale = src.DirectSale,
            Description = src.Description,
            DescriptionRichText = src.DescriptionRichText,
            InvoiceNoteTemplate = src.InvoiceNoteTemplate,
            WeightKg = src.WeightKg,
            SupplierName = src.SupplierName,
            ImageUrl = src.ImageUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _products.AddAsync(copy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(copy, src.Group);
    }

    public async Task<ProductListItemDto> UpdateStatusAsync(Guid id, UpdateProductStatusRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _products.GetTrackedByIdAsync(id, cancellationToken);
        if (item is null) throw new NotFoundException("Product not found.");
        item.RowStatus = request.Status;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<ProductListItemDto> UpdateGroupAsync(Guid id, UpdateProductGroupRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _products.GetTrackedByIdAsync(id, cancellationToken);
        if (item is null) throw new NotFoundException("Product not found.");
        var group = await ResolveGroupAsync(request.GroupName, cancellationToken);
        item.GroupId = group?.Id;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(item, group);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _products.GetTrackedByIdAsync(id, cancellationToken);
        if (item is null) return;
        item.IsDeleted = true;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductGroupDto>> ListGroupsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var groups = await _groups.ListAsync(cancellationToken);
        return groups.Select(x => new ProductGroupDto
        {
            Id = x.Id,
            Name = x.Name,
            ParentId = x.ParentId
        }).ToList();
    }

    public async Task<IReadOnlyList<ProductGroupTreeDto>> ListGroupTreeAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var groups = await _groups.ListAsync(cancellationToken);
        var counts = await _products.CountActiveByGroupAsync(cancellationToken);
        return ProductGroupTreeBuilder.Build(groups, counts);
    }

    public async Task<ProductGroupDto> CreateGroupAsync(CreateProductGroupRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        EnsureCanManagePriceListActions();
        var existing = await _groups.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null) return new ProductGroupDto { Id = existing.Id, Name = existing.Name };

        ProductGroup? parent = null;
        if (request.ParentId.HasValue)
        {
            parent = await _groups.GetTrackedByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent is null) throw new NotFoundException("Parent product group not found.");
            if (parent.IsDeleted) throw new ConflictException("Parent product group is inactive.");
        }

        var group = new ProductGroup
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ParentId = parent?.Id,
            CreatedDate = DateTime.UtcNow
        };
        await _groups.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ProductGroupDto { Id = group.Id, Name = group.Name };
    }

    public async Task<IReadOnlyList<PriceListDto>> ListPriceListsAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var lists = await _priceLists.ListAsync(search, cancellationToken);
        return lists.Select(Map).ToList();
    }

    public async Task<PriceListDto> CreatePriceListAsync(
        UpsertPriceListRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (await _priceLists.NameExistsAsync(request.Name.Trim(), null, cancellationToken))
            throw new ConflictException("Price list name already exists.");

        var entity = new PriceList
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Status = request.Status,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            FormulaSource = request.Formula.Source,
            FormulaOperation = request.Formula.Operation,
            FormulaValue = request.Formula.Value,
            FormulaUnit = request.Formula.Unit,
            RoundEnabled = request.Formula.RoundEnabled,
            RoundTo = request.Formula.RoundTo,
            SalesRuleMode = request.SalesRuleMode,
            BranchIdsJson = SerializeList(request.Scope.ApplyAllBranches ? [] : request.Scope.BranchIds),
            CustomerGroupIdsJson = SerializeList(request.Scope.ApplyAllCustomerGroups ? [] : request.Scope.CustomerGroupIds),
            CashierIdsJson = SerializeList(request.Scope.ApplyAllCashiers ? [] : request.Scope.CashierIds),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _priceLists.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<PriceListDto> UpdatePriceListAsync(
        Guid id,
        UpsertPriceListRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var entity = await _priceLists.GetTrackedByIdAsync(id, cancellationToken);
        if (entity is null) throw new NotFoundException("Price list not found.");
        if (await _priceLists.NameExistsAsync(request.Name.Trim(), id, cancellationToken))
            throw new ConflictException("Price list name already exists.");

        entity.Name = request.Name.Trim();
        entity.Status = request.Status;
        entity.StartAt = request.StartAt;
        entity.EndAt = request.EndAt;
        entity.FormulaSource = request.Formula.Source;
        entity.FormulaOperation = request.Formula.Operation;
        entity.FormulaValue = request.Formula.Value;
        entity.FormulaUnit = request.Formula.Unit;
        entity.RoundEnabled = request.Formula.RoundEnabled;
        entity.RoundTo = request.Formula.RoundTo;
        entity.SalesRuleMode = request.SalesRuleMode;
        entity.BranchIdsJson = SerializeList(request.Scope.ApplyAllBranches ? [] : request.Scope.BranchIds);
        entity.CustomerGroupIdsJson = SerializeList(request.Scope.ApplyAllCustomerGroups ? [] : request.Scope.CustomerGroupIds);
        entity.CashierIdsJson = SerializeList(request.Scope.ApplyAllCashiers ? [] : request.Scope.CashierIds);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<PriceListItemDto>> ListPriceListItemsAsync(
        PriceListItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (query.PriceListIds.Count == 0)
        {
            return Array.Empty<PriceListItemDto>();
        }

        string? legacyGroupId = query.GroupId;
        IReadOnlyList<Guid>? filterGroupGuids = null;
        if (query.GroupIds is { Count: > 0 })
        {
            var parsed = query.GroupIds.Where(g => g != Guid.Empty).Distinct().ToList();
            if (parsed.Count > 0)
            {
                var allGroups = await _groups.ListAsync(cancellationToken);
                filterGroupGuids = ExpandDescendantGroupIds(allGroups, parsed);
                legacyGroupId = null;
            }
        }

        var items = await _priceListItems.ListByPriceListIdsAsync(
            query.PriceListIds,
            query.Search,
            legacyGroupId,
            query.Stock,
            filterGroupGuids,
            cancellationToken);

        var result = items
            .Where(x => x.Product != null)
            .GroupBy(x => x.ProductId)
            .Select(group =>
            {
                var first = group.First();
                var product = first.Product!;
                return new PriceListItemDto
                {
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    CostPrice = product.CostPrice,
                    LastImportPrice = product.CostPrice,
                    PricesByListId = group
                        .GroupBy(x => x.PriceListId)
                        .ToDictionary(x => x.Key.ToString(), x => x.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt).First().Price)
                };
            })
            .OrderBy(x => x.ProductName)
            .ToList();
        if (query.PriceOperator is "lt" or "lte" or "eq" or "gt" or "gte"
            && query.ComparePrice is "costPrice" or "lastImportPrice"
            && query.CompareValue.HasValue)
        {
            result = result.Where(item =>
            {
                var compareBase = query.ComparePrice == "costPrice" ? item.CostPrice : item.LastImportPrice;
                var selectedPrice = item.PricesByListId.TryGetValue(query.PriceListIds[0].ToString(), out var value)
                    ? value
                    : item.PricesByListId.Values.FirstOrDefault();
                var delta = selectedPrice - compareBase;
                return query.PriceOperator switch
                {
                    "lt" => delta < query.CompareValue.Value,
                    "lte" => delta <= query.CompareValue.Value,
                    "eq" => delta == query.CompareValue.Value,
                    "gt" => delta > query.CompareValue.Value,
                    "gte" => delta >= query.CompareValue.Value,
                    _ => true
                };
            }).ToList();
        }
        return result;
    }

    public async Task SetPriceListItemManualAsync(
        Guid priceListId,
        Guid productId,
        decimal price,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        EnsureCanManagePriceListActions();

        var list = await _priceLists.GetByIdAsync(priceListId, cancellationToken);
        if (list is null) throw new NotFoundException("Price list not found.");

        var product = await _products.GetByIdAsync(productId, cancellationToken);
        if (product is null) throw new NotFoundException("Product not found.");

        var now = DateTimeOffset.UtcNow;
        var existing = await _priceListItems.GetTrackedAsync(priceListId, productId, cancellationToken);
        if (existing is null)
        {
            await _priceListItems.AddRangeAsync(
            [
                new PriceListItem
                {
                    Id = Guid.NewGuid(),
                    PriceListId = priceListId,
                    ProductId = productId,
                    Price = price,
                    AppliedByFormula = false,
                    CreatedAt = now
                }
            ], cancellationToken);
        }
        else
        {
            existing.Price = price;
            existing.AppliedByFormula = false;
            existing.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAllProductsToPriceListAsync(
        Guid priceListId,
        bool confirmed,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var list = await _priceLists.GetByIdAsync(priceListId, cancellationToken);
        if (list is null) throw new NotFoundException("Price list not found.");
        var existingItems = await _priceListItems.ListByPriceListIdsAsync(
            [priceListId],
            null,
            null,
            null,
            null,
            cancellationToken);
        if (existingItems.Count == 0 && !confirmed)
            throw new ConflictException("Confirmation required before adding all products to an empty price list.");

        var products = await _products.ListAllActiveAsync(cancellationToken);
        await UpsertPriceListItemsByFormulaAsync(list, products, cancellationToken);
    }

    public async Task AddProductsByGroupsToPriceListAsync(
        Guid priceListId,
        AddProductsByGroupsRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var list = await _priceLists.GetByIdAsync(priceListId, cancellationToken);
        if (list is null) throw new NotFoundException("Price list not found.");

        var groupIds = request.GroupIds
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        if (groupIds.Count == 0) return;
        if (request.IncludeDescendants)
        {
            var allGroups = await _groups.ListAsync(cancellationToken);
            groupIds = ExpandDescendantGroupIds(allGroups, groupIds);
        }

        var products = await _products.ListByGroupIdsAsync(groupIds, cancellationToken);
        await UpsertPriceListItemsByFormulaAsync(list, products, cancellationToken);
    }

    private static List<Guid> ExpandDescendantGroupIds(IReadOnlyList<ProductGroup> groups, IReadOnlyList<Guid> selected)
    {
        var childrenMap = groups
            .Where(g => g.ParentId.HasValue)
            .GroupBy(g => g.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());
        var expanded = new HashSet<Guid>(selected);
        var stack = new Stack<Guid>(selected);
        while (stack.Count > 0)
        {
            var parent = stack.Pop();
            if (!childrenMap.TryGetValue(parent, out var children))
                continue;

            foreach (var child in children.Where(expanded.Add))
                stack.Push(child);
        }

        return expanded.ToList();
    }

    public async Task ApplyPriceFormulaAsync(
        Guid priceListId,
        ApplyPriceFormulaRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var list = await _priceLists.GetByIdAsync(priceListId, cancellationToken);
        if (list is null) throw new NotFoundException("Price list not found.");

        IReadOnlyList<Product> products;
        if (request.ApplyTo == "single" && request.ProductId.HasValue)
        {
            var single = await _products.GetByIdAsync(request.ProductId.Value, cancellationToken);
            products = single is null ? [] : [single];
        }
        else
        {
            products = await _products.ListAllActiveAsync(cancellationToken);
        }

        await UpsertPriceListItemsByFormulaAsync(list, products, cancellationToken);
    }

    public async Task<PriceListImportResult> ImportPriceListAsync(
        PriceListImportRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        EnsureCanManagePriceListActions();

        if (request.FileBytes.Length == 0)
            throw new ConflictException("Import file is empty.");

        using var workbook = new XLWorkbook(new MemoryStream(request.FileBytes));
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
            throw new ConflictException("Template does not contain worksheet.");

        var headerMap = BuildHeaderMap(worksheet);
        var matrixImportResult = await TryImportPriceListMatrixAsync(request, worksheet, headerMap, cancellationToken);
        if (matrixImportResult is not null)
            return matrixImportResult;

        if (!headerMap.TryGetValue("productcode", out var productCodeCol)
            || !headerMap.TryGetValue("pricelistname", out var priceListNameCol)
            || !headerMap.TryGetValue("price", out var priceCol))
        {
            throw new ConflictException("Template headers are invalid.");
        }

        var totalRows = 0;
        var successfulRows = 0;
        var failedRows = 0;
        var errors = new List<PriceListImportError>();
        var now = DateTimeOffset.UtcNow;

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            totalRows++;
            var rowNumber = row.RowNumber();
            var productCode = row.Cell(productCodeCol).GetString().Trim();
            var priceListName = row.Cell(priceListNameCol).GetString().Trim();
            var priceRaw = row.Cell(priceCol).GetString().Trim();

            if (string.IsNullOrWhiteSpace(productCode) || string.IsNullOrWhiteSpace(priceListName))
            {
                failedRows++;
                errors.Add(new PriceListImportError
                {
                    RowNumber = rowNumber,
                    Field = "productCode|priceListName",
                    Message = "Product code and price list name are required."
                });
                continue;
            }

            if (!decimal.TryParse(priceRaw, out var price))
            {
                failedRows++;
                errors.Add(new PriceListImportError
                {
                    RowNumber = rowNumber,
                    Field = "price",
                    Message = "Price must be a number."
                });
                continue;
            }

            var products = await _products.ListAsync(new ProductQuery
            {
                Search = productCode,
                Page = 1,
                PageSize = 100
            }, cancellationToken);
            var product = products.Items.FirstOrDefault(x => string.Equals(x.Code, productCode, StringComparison.OrdinalIgnoreCase));
            var priceLists = await _priceLists.ListAsync(priceListName, cancellationToken);
            var priceList = priceLists.FirstOrDefault(x => string.Equals(x.Name, priceListName, StringComparison.OrdinalIgnoreCase));

            if (product is null || priceList is null)
            {
                failedRows++;
                errors.Add(new PriceListImportError
                {
                    RowNumber = rowNumber,
                    Message = product is null
                        ? $"Product code '{productCode}' not found."
                        : $"Price list '{priceListName}' not found."
                });
                continue;
            }

            var existing = await _priceListItems.GetTrackedAsync(priceList.Id, product.Id, cancellationToken);
            if (existing is null)
            {
                await _priceListItems.AddRangeAsync(new[]
                {
                    new PriceListItem
                    {
                        Id = Guid.NewGuid(),
                        PriceListId = priceList.Id,
                        ProductId = product.Id,
                        Price = price,
                        AppliedByFormula = false,
                        CreatedAt = now
                    }
                }, cancellationToken);
            }
            else
            {
                existing.Price = price;
                existing.AppliedByFormula = false;
                existing.UpdatedAt = now;
            }

            successfulRows++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PriceListImportResult
        {
            TotalRows = totalRows,
            SuccessfulRows = successfulRows,
            FailedRows = failedRows,
            Errors = errors
        };
    }

    public Task<byte[]> DownloadProductsImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        EnsureCanManagePriceListActions();
        return Task.FromResult(ProductImportTemplateBuilder.Build());
    }

    public async Task<ImportProductsExcelResult> ImportProductsExcelAsync(
        ImportProductsExcelRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        EnsureCanManagePriceListActions();

        if (request.FileBytes.Length == 0)
            throw new ConflictException("Import file is empty.");

        using var workbook = new XLWorkbook(new MemoryStream(request.FileBytes));
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
            throw new ConflictException("Template does not contain worksheet.");

        var headerMap = BuildProductImportHeaderMap(worksheet);
        if (!headerMap.ContainsKey("code") || !headerMap.ContainsKey("name"))
            throw new ConflictException("Template headers are invalid.");

        var validator = new UpsertProductRequestValidator();
        var allProducts = (await _products.ListAllActiveAsync(cancellationToken)).ToList();
        var byCode = allProducts
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .ToDictionary(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase);
        var byBarcode = allProducts
            .Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
            .ToDictionary(x => x.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase);
        var trackedCache = new Dictionary<Guid, Product>();

        var totalRows = 0;
        var importedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;
        var errors = new List<string>();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            totalRows++;
            var rowNumber = row.RowNumber();

            if (IsProductImportRowBlank(row, headerMap))
            {
                skippedCount++;
                continue;
            }

            ProductImportRowData rowData;
            try
            {
                rowData = ReadProductImportRow(row, headerMap);
            }
            catch (ConflictException ex)
            {
                failedCount++;
                errors.Add($"Dòng {rowNumber}: {ex.Message}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(rowData.Code) || string.IsNullOrWhiteSpace(rowData.Name))
            {
                failedCount++;
                errors.Add($"Dòng {rowNumber}: Mã hàng và tên hàng là bắt buộc.");
                continue;
            }

            var codeKey = rowData.Code.Trim();
            var barcodeKey = NormalizeNull(rowData.Barcode);
            var codeMatch = byCode.GetValueOrDefault(codeKey);
            var barcodeMatch = barcodeKey is null ? null : byBarcode.GetValueOrDefault(barcodeKey);

            if (codeMatch is not null && barcodeMatch is not null && codeMatch.Id != barcodeMatch.Id)
            {
                failedCount++;
                errors.Add($"Dòng {rowNumber}: Mã hàng và mã vạch đang trỏ tới 2 sản phẩm khác nhau, không thể tự động gộp.");
                continue;
            }

            Product? target = codeMatch ?? barcodeMatch;
            Product? trackedTarget = null;
            Product? trackedBarcodeOwner = null;

            if (target is null && barcodeMatch is not null)
            {
                target = barcodeMatch;
            }

            if (target is not null)
            {
                trackedTarget = await GetTrackedProductAsync(target.Id, trackedCache, cancellationToken);
            }

            if (barcodeMatch is not null)
            {
                trackedBarcodeOwner = await GetTrackedProductAsync(barcodeMatch.Id, trackedCache, cancellationToken);
            }

            if (barcodeMatch is not null
                && !string.Equals(barcodeMatch.Code, codeKey, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(request.DuplicateBarcodeConflictAction, ProductImportConflictAction.Replace, StringComparison.OrdinalIgnoreCase))
                {
                    failedCount++;
                    errors.Add($"Dòng {rowNumber}: Mã vạch '{barcodeKey}' đang thuộc mã hàng '{barcodeMatch.Code}'.");
                    continue;
                }

                if (codeMatch is not null && codeMatch.Id != barcodeMatch.Id)
                {
                    failedCount++;
                    errors.Add($"Dòng {rowNumber}: Không thể thay mã hàng vì mã mới '{codeKey}' đã tồn tại ở sản phẩm khác.");
                    continue;
                }

                target = barcodeMatch;
                trackedTarget = trackedBarcodeOwner;
            }

            if (trackedTarget is not null
                && !string.Equals(trackedTarget.Name.Trim(), rowData.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                && !string.Equals(request.DuplicateCodeConflictAction, ProductImportConflictAction.Replace, StringComparison.OrdinalIgnoreCase))
            {
                failedCount++;
                errors.Add($"Dòng {rowNumber}: Sản phẩm '{codeKey}' có tên hiện tại khác tên trong file import.");
                continue;
            }

            UpsertProductRequest upsertRequest;
            if (trackedTarget is null)
            {
                upsertRequest = BuildCreateRequest(rowData);
            }
            else
            {
                upsertRequest = BuildUpdateRequest(trackedTarget, rowData, request);
                if (barcodeMatch is not null
                    && barcodeMatch.Id == trackedTarget.Id
                    && !string.Equals(barcodeMatch.Code, codeKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(request.DuplicateBarcodeConflictAction, ProductImportConflictAction.Replace, StringComparison.OrdinalIgnoreCase))
                {
                    upsertRequest.Code = codeKey;
                }
            }

            var validation = validator.Validate(upsertRequest);
            if (!validation.IsValid)
            {
                failedCount++;
                errors.Add($"Dòng {rowNumber}: {string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))}");
                continue;
            }

            if (trackedTarget is null)
            {
                var group = await ResolveGroupAsync(upsertRequest.GroupName, cancellationToken);
                var now = DateTimeOffset.UtcNow;
                var entity = new Product
                {
                    Id = Guid.NewGuid(),
                    Code = upsertRequest.Code!.Trim(),
                    Name = upsertRequest.Name.Trim(),
                    ItemType = upsertRequest.ItemType,
                    Barcode = NormalizeNull(upsertRequest.Barcode),
                    GroupId = group?.Id,
                    Group = group,
                    Brand = NormalizeNull(upsertRequest.Brand),
                    CostPrice = upsertRequest.CostPrice,
                    SalePrice = upsertRequest.SalePrice,
                    Stock = upsertRequest.Stock,
                    MinStock = upsertRequest.MinStock,
                    MaxStock = upsertRequest.MaxStock,
                    Location = NormalizeNull(upsertRequest.Location),
                    RowStatus = "active",
                    DirectSale = upsertRequest.DirectSale,
                    Description = NormalizeNull(upsertRequest.Description),
                    DescriptionRichText = NormalizeNull(upsertRequest.DescriptionRichText),
                    InvoiceNoteTemplate = NormalizeNull(upsertRequest.InvoiceNoteTemplate),
                    WeightKg = ToWeightKg(upsertRequest.WeightValue, upsertRequest.WeightUnit),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _products.AddAsync(entity, cancellationToken);
                trackedCache[entity.Id] = entity;
                allProducts.Add(entity);
                byCode[entity.Code] = entity;
                if (!string.IsNullOrWhiteSpace(entity.Barcode))
                    byBarcode[entity.Barcode] = entity;
                importedCount++;
                continue;
            }

            var oldCode = trackedTarget.Code;
            var oldBarcode = NormalizeNull(trackedTarget.Barcode);
            var updatedGroup = await ResolveGroupAsync(upsertRequest.GroupName, cancellationToken);

            trackedTarget.Code = upsertRequest.Code!.Trim();
            trackedTarget.Name = upsertRequest.Name.Trim();
            trackedTarget.ItemType = upsertRequest.ItemType;
            trackedTarget.Barcode = NormalizeNull(upsertRequest.Barcode);
            trackedTarget.GroupId = updatedGroup?.Id;
            trackedTarget.Group = updatedGroup;
            trackedTarget.Brand = NormalizeNull(upsertRequest.Brand);
            trackedTarget.CostPrice = upsertRequest.CostPrice;
            trackedTarget.SalePrice = upsertRequest.SalePrice;
            trackedTarget.Stock = upsertRequest.Stock;
            trackedTarget.MinStock = upsertRequest.MinStock;
            trackedTarget.MaxStock = upsertRequest.MaxStock;
            trackedTarget.Location = NormalizeNull(upsertRequest.Location);
            trackedTarget.DirectSale = upsertRequest.DirectSale;
            trackedTarget.Description = NormalizeNull(upsertRequest.Description);
            trackedTarget.DescriptionRichText = NormalizeNull(upsertRequest.DescriptionRichText);
            trackedTarget.InvoiceNoteTemplate = NormalizeNull(upsertRequest.InvoiceNoteTemplate);
            trackedTarget.WeightKg = ToWeightKg(upsertRequest.WeightValue, upsertRequest.WeightUnit);
            trackedTarget.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.Equals(oldCode, trackedTarget.Code, StringComparison.OrdinalIgnoreCase))
                byCode.Remove(oldCode);
            byCode[trackedTarget.Code] = trackedTarget;

            if (oldBarcode is not null && !string.Equals(oldBarcode, trackedTarget.Barcode, StringComparison.OrdinalIgnoreCase))
                byBarcode.Remove(oldBarcode);
            if (!string.IsNullOrWhiteSpace(trackedTarget.Barcode))
                byBarcode[trackedTarget.Barcode] = trackedTarget;

            updatedCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportProductsExcelResult
        {
            TotalRows = totalRows,
            ImportedCount = importedCount,
            UpdatedCount = updatedCount,
            SkippedCount = skippedCount,
            FailedCount = failedCount,
            Errors = errors
        };
    }

    public async Task<byte[]?> ExportProductsExcelAsync(
        ExportProductsExcelRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        EnsureCanManagePriceListActions();

        var columns = request.Columns
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.HeaderName))
            .ToList();
        if (columns.Count == 0)
            throw new ConflictException("Vui lòng chọn ít nhất 1 cột để xuất file.");

        var query = new ProductQuery
        {
            Search = request.Search,
            GroupId = request.GroupId,
            GroupIds = request.GroupIds,
            Stock = request.Stock,
            DirectSale = request.DirectSale,
            Status = request.Status,
            CreatedFrom = request.CreatedFrom,
            CreatedTo = request.CreatedTo,
            ExpectedFrom = request.ExpectedFrom,
            ExpectedTo = request.ExpectedTo
        };

        var products = await ListAllProductsForExportAsync(query, cancellationToken);
        if (products.Count == 0)
            return null;

        var groups = await _groups.ListAsync(cancellationToken);
        var groupNameById = groups.ToDictionary(x => x.Id, x => x.Name);
        var groupParentById = groups.ToDictionary(x => x.Id, x => x.ParentId);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        for (var c = 0; c < columns.Count; c++)
        {
            ws.Cell(1, c + 1).Value = columns[c].HeaderName;
        }

        for (var i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var row = i + 2;
            for (var c = 0; c < columns.Count; c++)
            {
                ws.Cell(row, c + 1).Value = GetExportCellValue(product, columns[c].Key, groupNameById, groupParentById);
            }
        }

        var header = ws.Range(1, 1, 1, columns.Count);
        header.Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]?> ExportPriceListAsync(
        PriceListItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        EnsureCanManagePriceListActions();

        var items = await ListPriceListItemsAsync(query, cancellationToken);
        if (items.Count == 0) return null;
        var selectedPriceLists = new List<PriceList>();
        foreach (var priceListId in query.PriceListIds)
        {
            var priceList = await _priceLists.GetByIdAsync(priceListId, cancellationToken);
            if (priceList is not null)
                selectedPriceLists.Add(priceList);
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("PriceListExport");
        ws.Cell(1, 1).Value = "Mã hàng";
        ws.Cell(1, 2).Value = "Tên hàng";
        ws.Cell(1, 3).Value = "Giá vốn";
        ws.Cell(1, 4).Value = "Giá nhập cuối";
        for (var i = 0; i < query.PriceListIds.Count; i++)
        {
            var fallbackName = $"Bảng giá {i + 1}";
            var priceListName = selectedPriceLists.FirstOrDefault(x => x.Id == query.PriceListIds[i])?.Name ?? fallbackName;
            ws.Cell(1, 5 + i).Value = priceListName;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var row = i + 2;
            var item = items[i];
            ws.Cell(row, 1).Value = item.ProductCode;
            ws.Cell(row, 2).Value = item.ProductName;
            ws.Cell(row, 3).Value = item.CostPrice;
            ws.Cell(row, 4).Value = item.LastImportPrice;
            for (var priceListIndex = 0; priceListIndex < query.PriceListIds.Count; priceListIndex++)
            {
                var key = query.PriceListIds[priceListIndex].ToString();
                if (item.PricesByListId.TryGetValue(key, out var price))
                    ws.Cell(row, 5 + priceListIndex).Value = price;
            }
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private async Task UpsertPriceListItemsByFormulaAsync(
        PriceList list,
        IReadOnlyList<Product> products,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var product in products)
        {
            var existing = await _priceListItems.GetTrackedAsync(list.Id, product.Id, cancellationToken);
            var computedPrice = ComputePrice(list, product);
            if (existing is null)
            {
                await _priceListItems.AddRangeAsync(
                [
                    new PriceListItem
                    {
                        Id = Guid.NewGuid(),
                        PriceListId = list.Id,
                        ProductId = product.Id,
                        Price = computedPrice,
                        AppliedByFormula = true,
                        CreatedAt = now
                    }
                ], cancellationToken);
                continue;
            }

            existing.Price = computedPrice;
            existing.AppliedByFormula = true;
            existing.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Product>> ListAllProductsForExportAsync(
        ProductQuery baseQuery,
        CancellationToken cancellationToken)
    {
        var results = new List<Product>();
        var page = 1;
        while (true)
        {
            var pageResult = await _products.ListAsync(new ProductQuery
            {
                Search = baseQuery.Search,
                GroupId = baseQuery.GroupId,
                Stock = baseQuery.Stock,
                DirectSale = baseQuery.DirectSale,
                Status = baseQuery.Status,
                CreatedFrom = baseQuery.CreatedFrom,
                CreatedTo = baseQuery.CreatedTo,
                ExpectedFrom = baseQuery.ExpectedFrom,
                ExpectedTo = baseQuery.ExpectedTo,
                Page = page,
                PageSize = 500
            }, cancellationToken);

            if (pageResult.Items.Count == 0)
                break;

            results.AddRange(pageResult.Items);

            if (results.Count >= pageResult.Total || pageResult.Items.Count < 500)
                break;

            page++;
        }

        return results;
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }

    private void EnsureCanManagePriceListActions()
    {
        if (_currentUser.IsAdmin)
            return;

        var allowedRoles = new[] { "admin", "product_manager", "price_settings_manager" };
        if (_currentUser.Roles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
            return;

        throw new ForbiddenException("No permission to perform this action.");
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in worksheet.Row(1).CellsUsed())
        {
            var key = NormalizeHeader(cell.GetString());
            if (key is "mãhàng" or "mahang" or "productcode")
                map["productcode"] = cell.Address.ColumnNumber;
            else if (key is "tênhàng" or "tenhang" or "productname")
                map["productname"] = cell.Address.ColumnNumber;
            else if (key is "tênbảnggiá" or "tenbanggia" or "banggia")
                map["pricelistname"] = cell.Address.ColumnNumber;
            else if (key is "giá" or "gia" or "price")
                map["price"] = cell.Address.ColumnNumber;
            else if (key.StartsWith("tênbảnggiá", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("tenbanggia", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("pricecolumn", StringComparison.OrdinalIgnoreCase))
                map[$"pricecolumn:{cell.Address.ColumnNumber}"] = cell.Address.ColumnNumber;
        }

        return map;
    }

    private async Task<PriceListImportResult?> TryImportPriceListMatrixAsync(
        PriceListImportRequest request,
        IXLWorksheet worksheet,
        IReadOnlyDictionary<string, int> headerMap,
        CancellationToken cancellationToken)
    {
        if (!headerMap.TryGetValue("productcode", out var productCodeCol)
            || !headerMap.TryGetValue("productname", out _))
        {
            return null;
        }

        var priceColumns = headerMap
            .Where(x => x.Key.StartsWith("pricecolumn:", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Value)
            .Select(x => x.Value)
            .ToList();
        if (priceColumns.Count == 0)
            return null;

        var selectedPriceListIds = request.SelectedPriceListIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        if (selectedPriceListIds.Count == 0)
            throw new ConflictException("Vui lòng chọn ít nhất một bảng giá trước khi import.");
        if (selectedPriceListIds.Count > priceColumns.Count)
            throw new ConflictException($"File mẫu chỉ hỗ trợ tối đa {priceColumns.Count} bảng giá mỗi lần import.");

        var selectedPriceLists = new List<PriceList>();
        foreach (var priceListId in selectedPriceListIds)
        {
            var priceList = await _priceLists.GetByIdAsync(priceListId, cancellationToken);
            if (priceList is null)
                throw new NotFoundException($"Price list '{priceListId}' not found.");
            selectedPriceLists.Add(priceList);
        }

        var allProducts = await _products.ListAllActiveAsync(cancellationToken);
        var productsByCode = allProducts
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .ToDictionary(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase);

        var totalRows = 0;
        var successfulRows = 0;
        var failedRows = 0;
        var errors = new List<PriceListImportError>();
        var now = DateTimeOffset.UtcNow;

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            totalRows++;
            var rowNumber = row.RowNumber();
            var productCode = row.Cell(productCodeCol).GetString().Trim();

            if (string.IsNullOrWhiteSpace(productCode))
            {
                failedRows++;
                errors.Add(new PriceListImportError
                {
                    RowNumber = rowNumber,
                    Field = "productCode",
                    Message = "Product code is required."
                });
                continue;
            }

            if (!productsByCode.TryGetValue(productCode, out var product))
            {
                failedRows++;
                errors.Add(new PriceListImportError
                {
                    RowNumber = rowNumber,
                    Field = "productCode",
                    Message = $"Product code '{productCode}' not found."
                });
                continue;
            }

            var parsedPrices = new List<(Guid PriceListId, decimal Price)>();
            var hasInvalidPrice = false;
            for (var index = 0; index < selectedPriceLists.Count; index++)
            {
                var rawValue = row.Cell(priceColumns[index]).GetString().Trim();
                if (string.IsNullOrWhiteSpace(rawValue))
                    continue;

                if (!decimal.TryParse(rawValue, out var price))
                {
                    failedRows++;
                    hasInvalidPrice = true;
                    errors.Add(new PriceListImportError
                    {
                        RowNumber = rowNumber,
                        Field = $"priceList[{index + 1}]",
                        Message = "Price must be a number."
                    });
                    break;
                }

                parsedPrices.Add((selectedPriceLists[index].Id, price));
            }

            if (hasInvalidPrice)
                continue;

            if (parsedPrices.Count == 0)
            {
                failedRows++;
                errors.Add(new PriceListImportError
                {
                    RowNumber = rowNumber,
                    Field = "price",
                    Message = "At least one selected price list value is required."
                });
                continue;
            }

            foreach (var (priceListId, price) in parsedPrices)
            {
                var existing = await _priceListItems.GetTrackedAsync(priceListId, product.Id, cancellationToken);
                if (existing is null)
                {
                    await _priceListItems.AddRangeAsync(
                    [
                        new PriceListItem
                        {
                            Id = Guid.NewGuid(),
                            PriceListId = priceListId,
                            ProductId = product.Id,
                            Price = price,
                            AppliedByFormula = false,
                            CreatedAt = now
                        }
                    ], cancellationToken);
                }
                else
                {
                    existing.Price = price;
                    existing.AppliedByFormula = false;
                    existing.UpdatedAt = now;
                }
            }

            successfulRows++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PriceListImportResult
        {
            TotalRows = totalRows,
            SuccessfulRows = successfulRows,
            FailedRows = failedRows,
            Errors = errors
        };
    }

    private static Dictionary<string, int> BuildProductImportHeaderMap(IXLWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in worksheet.Row(1).CellsUsed())
        {
            var key = NormalizeHeader(cell.GetString());
            switch (key)
            {
                case "mahang":
                case "mãhàng":
                case "productcode":
                case "code":
                    map["code"] = cell.Address.ColumnNumber;
                    break;
                case "tenhang":
                case "tênhàng":
                case "tenhanghoa":
                case "tênhànghóa":
                case "productname":
                case "name":
                    map["name"] = cell.Address.ColumnNumber;
                    break;
                case "mavach":
                case "mãvạch":
                case "barcode":
                    map["barcode"] = cell.Address.ColumnNumber;
                    break;
                case "nhomhang":
                case "nhómhàng":
                case "nhomhang3cap":
                case "nhómhàng3cấp":
                case "group":
                case "groupname":
                case "groupname3level":
                    map["groupName"] = cell.Address.ColumnNumber;
                    break;
                case "thuonghieu":
                case "thươnghiệu":
                case "brand":
                    map["brand"] = cell.Address.ColumnNumber;
                    break;
                case "giavon":
                case "giávốn":
                case "costprice":
                    map["costPrice"] = cell.Address.ColumnNumber;
                    break;
                case "giaban":
                case "giábán":
                case "saleprice":
                    map["salePrice"] = cell.Address.ColumnNumber;
                    break;
                case "tonkho":
                case "tồnkho":
                case "stock":
                    map["stock"] = cell.Address.ColumnNumber;
                    break;
                case "tontoithieu":
                case "tồntốithiểu":
                case "tonnhonhat":
                case "tồnnhỏnhất":
                case "minstock":
                    map["minStock"] = cell.Address.ColumnNumber;
                    break;
                case "tontoida":
                case "tồntốiđa":
                case "tonlonnhat":
                case "tồnlớnhất":
                case "maxstock":
                    map["maxStock"] = cell.Address.ColumnNumber;
                    break;
                case "vitri":
                case "vịtrí":
                case "location":
                    map["location"] = cell.Address.ColumnNumber;
                    break;
                case "khoiluong":
                case "khốilượng":
                case "weight":
                case "weightvalue":
                    map["weightValue"] = cell.Address.ColumnNumber;
                    break;
                case "donvikhoiluong":
                case "đơnvịkhốilượng":
                case "weightunit":
                    map["weightUnit"] = cell.Address.ColumnNumber;
                    break;
                case "mota":
                case "môtả":
                case "description":
                    map["description"] = cell.Address.ColumnNumber;
                    break;
                case "bantructiep":
                case "bántrựctiếp":
                case "duocbantructiep":
                case "đượcbántrựctiếp":
                case "directsale":
                    map["directSale"] = cell.Address.ColumnNumber;
                    break;
                case "loaihang":
                case "loạihàng":
                case "itemtype":
                case "producttype":
                    map["itemType"] = cell.Address.ColumnNumber;
                    break;
                case "hinhanh":
                case "hìnhảnh":
                case "hinhanhurl1url2":
                case "hìnhảnhurl1url2":
                case "image":
                case "images":
                case "imageurl":
                case "imageurls":
                    map["imageUrls"] = cell.Address.ColumnNumber;
                    break;
                case "trongluong":
                case "trọnglượng":
                    map["weightValue"] = cell.Address.ColumnNumber;
                    break;
                case "dangkinhdoanh":
                case "đangkinhdoanh":
                case "isactive":
                    map["isActive"] = cell.Address.ColumnNumber;
                    break;
                case "maughichu":
                case "mẫughichú":
                case "invoicenotetemplate":
                    map["invoiceNoteTemplate"] = cell.Address.ColumnNumber;
                    break;
            }
        }

        return map;
    }

    private async Task<string> ResolveCodeAsync(string? requestedCode, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedCode))
        {
            var code = requestedCode.Trim();
            if (await _products.CodeExistsAsync(code, excludeId, cancellationToken))
                throw new ConflictException("Product code already exists.");
            return code;
        }

        for (var i = 0; i < 10000; i++)
        {
            var candidate = $"SP{DateTime.UtcNow:yyMMdd}{Random.Shared.Next(1000, 9999)}";
            if (!await _products.CodeExistsAsync(candidate, excludeId, cancellationToken))
                return candidate;
        }

        throw new ConflictException("Cannot generate product code, please retry.");
    }

    private async Task EnsureUniqueBarcodeAsync(string? barcode, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return;
        if (await _products.BarcodeExistsAsync(barcode.Trim(), excludeId, cancellationToken))
            throw new ConflictException("Product barcode already exists.");
    }

    private async Task<ProductGroup?> ResolveGroupAsync(string? groupName, CancellationToken cancellationToken)
    {
        var value = NormalizeNull(groupName);
        if (value is null) return null;

        var segments = value
            .Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        if (segments.Count == 0) return null;

        ProductGroup? parent = null;
        ProductGroup? current = null;
        foreach (var segment in segments)
        {
            current = await _groups.GetByNameAsync(segment, cancellationToken);
            if (current is null)
            {
                current = new ProductGroup
                {
                    Id = Guid.NewGuid(),
                    Name = segment,
                    ParentId = parent?.Id,
                    CreatedDate = DateTime.UtcNow
                };
                await _groups.AddAsync(current, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else if (parent is not null && current.ParentId != parent.Id && current.ParentId is null)
            {
                // Keep existing parent if already set; only attach orphans under path parent.
                current.ParentId = parent.Id;
                current.UpdatedDate = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            parent = current;
        }

        return current;
    }

    private static string? ParseFirstImageUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var first = raw
            .Split([',', ';', '|', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return ProductMappings.NormalizeDisplayImageUrl(first);
    }

    private static ProductListItemDto Map(Product x, ProductGroup? g = null, decimal? salePriceInPriceList = null)
    {
        var group = g ?? x.Group;
        return new ProductListItemDto
        {
            Id = x.Id,
            ImageUrl = ProductMappings.NormalizeDisplayImageUrl(x.ImageUrl),
            Code = x.Code,
            Name = x.Name,
            ItemType = x.ItemType,
            SalePrice = x.SalePrice,
            SalePriceInPriceList = salePriceInPriceList,
            CostPrice = x.CostPrice,
            Stock = x.Stock,
            CustomerOrders = 0,
            CreatedAt = x.CreatedAt,
            ExpectedStockoutAt = x.ExpectedStockoutAt,
            SupplierOrderQty = x.SupplierOrderQty,
            IsFavorite = x.IsFavorite,
            Barcode = x.Barcode,
            GroupId = group?.Id.ToString(),
            GroupName = group?.Name,
            ProductType = x.ItemType switch
            {
                "combo" => "Combo - đóng gói",
                "service" => "Dịch vụ",
                _ => "Hàng hóa thường"
            },
            ChannelLinked = x.ChannelLinked,
            Brand = x.Brand,
            Location = x.Location,
            MinStock = x.MinStock,
            MaxStock = x.MaxStock,
            RowStatus = x.RowStatus,
            DirectSale = x.DirectSale,
            Description = x.Description,
            Note = x.InvoiceNoteTemplate,
            InvoiceNoteTemplate = x.InvoiceNoteTemplate,
            DescriptionRichText = x.DescriptionRichText,
            WeightKg = x.WeightKg,
            SupplierName = x.SupplierName
        };
    }

    private static decimal? ToWeightKg(decimal value, string unit)
    {
        if (value <= 0) return 0;
        return unit == "kg" ? value : value / 1000m;
    }

    private static string? NormalizeNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeHeader(string? value)
    {
        var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
        var buffer = new char[raw.Length];
        var n = 0;
        foreach (var ch in raw)
        {
            if (char.IsLetterOrDigit(ch))
                buffer[n++] = ch;
        }

        return new string(buffer, 0, n);
    }

    private static bool IsProductImportRowBlank(IXLRow row, IReadOnlyDictionary<string, int> headerMap)
    {
        foreach (var col in headerMap.Values)
        {
            if (!string.IsNullOrWhiteSpace(row.Cell(col).GetString()))
                return false;
        }

        return true;
    }

    private async Task<Product> GetTrackedProductAsync(
        Guid productId,
        IDictionary<Guid, Product> trackedCache,
        CancellationToken cancellationToken)
    {
        if (trackedCache.TryGetValue(productId, out var cached))
            return cached;

        var tracked = await _products.GetTrackedByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        trackedCache[productId] = tracked;
        return tracked;
    }

    private static ProductImportRowData ReadProductImportRow(IXLRow row, IReadOnlyDictionary<string, int> headerMap)
    {
        return new ProductImportRowData
        {
            Code = ReadRequiredString(row, headerMap, "code"),
            Name = ReadRequiredString(row, headerMap, "name"),
            Barcode = ReadOptionalString(row, headerMap, "barcode"),
            GroupName = ReadOptionalString(row, headerMap, "groupName"),
            Brand = ReadOptionalString(row, headerMap, "brand"),
            CostPrice = ReadOptionalDecimal(row, headerMap, "costPrice"),
            SalePrice = ReadOptionalDecimal(row, headerMap, "salePrice"),
            Stock = ReadOptionalInt(row, headerMap, "stock"),
            MinStock = ReadOptionalInt(row, headerMap, "minStock"),
            MaxStock = ReadOptionalInt(row, headerMap, "maxStock"),
            Location = ReadOptionalString(row, headerMap, "location"),
            WeightValue = ReadOptionalDecimal(row, headerMap, "weightValue"),
            WeightUnit = ReadOptionalString(row, headerMap, "weightUnit"),
            Description = ReadOptionalString(row, headerMap, "description"),
            DirectSale = ReadOptionalBool(row, headerMap, "directSale"),
            ItemType = ReadOptionalString(row, headerMap, "itemType"),
            ImageUrls = ReadOptionalString(row, headerMap, "imageUrls"),
            InvoiceNoteTemplate = ReadOptionalString(row, headerMap, "invoiceNoteTemplate")
        };
    }

    private static UpsertProductRequest BuildCreateRequest(ProductImportRowData row)
    {
        var imageUrl = ParseFirstImageUrl(row.ImageUrls);
        return new UpsertProductRequest
        {
            Code = row.Code.Trim(),
            Name = row.Name.Trim(),
            ItemType = NormalizeItemType(row.ItemType) ?? "goods",
            Barcode = NormalizeNull(row.Barcode),
            GroupName = row.GroupName,
            Brand = row.Brand,
            CostPrice = row.CostPrice ?? 0,
            SalePrice = row.SalePrice ?? 0,
            Stock = row.Stock ?? 0,
            MinStock = row.MinStock ?? 0,
            MaxStock = row.MaxStock ?? Math.Max(row.MinStock ?? 0, 0),
            Location = row.Location,
            WeightValue = row.WeightValue ?? 0,
            WeightUnit = NormalizeWeightUnit(row.WeightUnit) ?? "g",
            Description = row.Description,
            InvoiceNoteTemplate = row.InvoiceNoteTemplate,
            DirectSale = row.DirectSale ?? false,
            Images = string.IsNullOrWhiteSpace(imageUrl) ? Array.Empty<string>() : [imageUrl],
            UploadedImageAssets = Array.Empty<UploadedImageReferenceDto>(),
            ComboComponents = Array.Empty<ProductComboComponentDto>()
        };
    }

    private static UpsertProductRequest BuildUpdateRequest(
        Product existing,
        ProductImportRowData row,
        ImportProductsExcelRequest options)
    {
        var request = new UpsertProductRequest
        {
            Code = row.Code.Trim(),
            Name = row.Name.Trim(),
            ItemType = NormalizeItemType(row.ItemType) ?? existing.ItemType,
            Barcode = row.Barcode is not null ? NormalizeNull(row.Barcode) : existing.Barcode,
            GroupName = row.GroupName is not null ? row.GroupName : existing.Group?.Name,
            Brand = row.Brand is not null ? row.Brand : existing.Brand,
            CostPrice = options.UpdateCostPrice ? row.CostPrice ?? existing.CostPrice : existing.CostPrice,
            SalePrice = row.SalePrice ?? existing.SalePrice,
            Stock = options.UpdateStock ? row.Stock ?? existing.Stock : existing.Stock,
            MinStock = row.MinStock ?? existing.MinStock ?? 0,
            MaxStock = row.MaxStock ?? existing.MaxStock ?? Math.Max(row.MinStock ?? existing.MinStock ?? 0, 0),
            Location = row.Location is not null ? row.Location : existing.Location,
            WeightValue = row.WeightValue ?? existing.WeightKg ?? 0,
            WeightUnit = NormalizeWeightUnit(row.WeightUnit) ?? "kg",
            Description = options.UpdateDescription ? row.Description : existing.Description,
            DescriptionRichText = existing.DescriptionRichText,
            InvoiceNoteTemplate = row.InvoiceNoteTemplate is not null
                ? row.InvoiceNoteTemplate
                : existing.InvoiceNoteTemplate,
            DirectSale = row.DirectSale ?? existing.DirectSale,
            Images = string.IsNullOrWhiteSpace(ParseFirstImageUrl(row.ImageUrls))
                ? (string.IsNullOrWhiteSpace(existing.ImageUrl) ? Array.Empty<string>() : [existing.ImageUrl])
                : [ParseFirstImageUrl(row.ImageUrls)!],
            UploadedImageAssets = Array.Empty<UploadedImageReferenceDto>(),
            ComboComponents = Array.Empty<ProductComboComponentDto>()
        };

        if (!options.UpdateDescription)
            request.Description = existing.Description;

        return request;
    }

    private static string ReadRequiredString(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var col))
            return string.Empty;

        return row.Cell(col).GetString().Trim();
    }

    private static string? ReadOptionalString(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var col))
            return null;

        return NormalizeNull(row.Cell(col).GetString());
    }

    private static decimal? ReadOptionalDecimal(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var col))
            return null;

        var raw = row.Cell(col).GetString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (!decimal.TryParse(raw, out var value))
            throw new ConflictException($"Cột '{key}' phải là số.");
        return value;
    }

    private static int? ReadOptionalInt(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var col))
            return null;

        var raw = row.Cell(col).GetString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (!int.TryParse(raw, out var value))
            throw new ConflictException($"Cột '{key}' phải là số nguyên.");
        return value;
    }

    private static bool? ReadOptionalBool(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var col))
            return null;

        var raw = row.Cell(col).GetString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return NormalizeHeader(raw) switch
        {
            "1" or "true" or "co" or "có" or "yes" or "x" => true,
            "0" or "false" or "khong" or "không" or "no" => false,
            _ => throw new ConflictException($"Cột '{key}' phải là giá trị đúng/sai.")
        };
    }

    private static string? NormalizeItemType(string? value)
    {
        var key = NormalizeHeader(value);
        return key switch
        {
            "" => null,
            "goods" or "hanghoa" or "hanghoathuong" => "goods",
            "service" or "dichvu" => "service",
            "combo" => "combo",
            _ => value
        };
    }

    private static string? NormalizeWeightUnit(string? value)
    {
        var key = NormalizeHeader(value);
        return key switch
        {
            "" => null,
            "g" or "gram" => "g",
            "kg" => "kg",
            _ => value
        };
    }

    private static string GetExportCellValue(
        Product product,
        string rawKey,
        IReadOnlyDictionary<Guid, string> groupNameById,
        IReadOnlyDictionary<Guid, Guid?> groupParentById)
    {
        var key = rawKey.Trim();
        return key switch
        {
            "itemType" => GetExportItemType(product.ItemType),
            "code" => product.Code,
            "name" => product.Name,
            "imageUrls" => ProductMappings.NormalizeDisplayImageUrl(product.ImageUrl) ?? string.Empty,
            "directSale" => product.DirectSale ? "Có" : "Không",
            "groupName3Level" => BuildGroupPath(product.GroupId, groupNameById, groupParentById),
            "barcode" => product.Barcode ?? string.Empty,
            "brand" => product.Brand ?? string.Empty,
            "isActive" => string.Equals(product.RowStatus, "active", StringComparison.OrdinalIgnoreCase) ? "Có" : "Không",
            "salePrice" => product.SalePrice.ToString("0.##"),
            "stock" => product.Stock.ToString(),
            "customerOrders" => "0",
            "minStock" => (product.MinStock ?? 0).ToString(),
            "costPrice" => product.CostPrice.ToString("0.##"),
            "supplierOrderQty" => product.SupplierOrderQty.ToString(),
            "expectedStockoutAt" => product.ExpectedStockoutAt?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty,
            "maxStock" => (product.MaxStock ?? 0).ToString(),
            "unitName" => string.Empty,
            "conversionValue" => string.Empty,
            "baseUnitCode" => string.Empty,
            "attributes" => string.Empty,
            "relatedProductCode" => string.Empty,
            "weightKg" => product.WeightKg?.ToString("0.###") ?? string.Empty,
            "description" => product.Description ?? string.Empty,
            "comboComponents" => string.Empty,
            "location" => product.Location ?? string.Empty,
            "invoiceNoteTemplate" => product.InvoiceNoteTemplate ?? string.Empty,
            "createdAt" => product.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            _ => string.Empty
        };
    }

    private static string GetExportItemType(string itemType) =>
        itemType switch
        {
            "combo" => "Combo - đóng gói",
            "service" => "Dịch vụ",
            _ => "Hàng hóa"
        };

    private static string BuildGroupPath(
        Guid? groupId,
        IReadOnlyDictionary<Guid, string> groupNameById,
        IReadOnlyDictionary<Guid, Guid?> groupParentById)
    {
        if (!groupId.HasValue)
            return string.Empty;

        var parts = new List<string>();
        var cursor = groupId;
        var depth = 0;
        while (cursor.HasValue && depth < 10)
        {
            if (groupNameById.TryGetValue(cursor.Value, out var name) && !string.IsNullOrWhiteSpace(name))
                parts.Add(name);
            cursor = groupParentById.GetValueOrDefault(cursor.Value);
            depth++;
        }

        parts.Reverse();
        return string.Join(" > ", parts);
    }

    private sealed class ProductImportRowData
    {
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Barcode { get; init; }
        public string? GroupName { get; init; }
        public string? Brand { get; init; }
        public decimal? CostPrice { get; init; }
        public decimal? SalePrice { get; init; }
        public int? Stock { get; init; }
        public int? MinStock { get; init; }
        public int? MaxStock { get; init; }
        public string? Location { get; init; }
        public decimal? WeightValue { get; init; }
        public string? WeightUnit { get; init; }
        public string? Description { get; init; }
        public bool? DirectSale { get; init; }
        public string? ItemType { get; init; }
        public string? ImageUrls { get; init; }
        public string? InvoiceNoteTemplate { get; init; }
    }

    private static PriceListDto Map(PriceList x) =>
        new()
        {
            Id = x.Id,
            Name = x.Name,
            Status = x.Status,
            StartAt = x.StartAt,
            EndAt = x.EndAt,
            Formula = new PriceListFormulaDto
            {
                Source = x.FormulaSource,
                Operation = x.FormulaOperation,
                Value = x.FormulaValue,
                Unit = x.FormulaUnit,
                RoundEnabled = x.RoundEnabled,
                RoundTo = x.RoundTo
            },
            SalesRuleMode = x.SalesRuleMode,
            Scope = new PriceListScopeDto
            {
                ApplyAllBranches = DeserializeList(x.BranchIdsJson).Count == 0,
                BranchIds = DeserializeList(x.BranchIdsJson),
                ApplyAllCustomerGroups = DeserializeList(x.CustomerGroupIdsJson).Count == 0,
                CustomerGroupIds = DeserializeList(x.CustomerGroupIdsJson),
                ApplyAllCashiers = DeserializeList(x.CashierIdsJson).Count == 0,
                CashierIds = DeserializeList(x.CashierIdsJson)
            }
        };

    private static decimal ComputePrice(PriceList list, Product product)
    {
        var basePrice = list.FormulaSource switch
        {
            "costPrice" => product.CostPrice,
            "lastImportPrice" => product.CostPrice,
            "basePriceList" => product.SalePrice,
            _ => product.SalePrice
        };
        return PriceFormulaCalculator.Calculate(
            basePrice,
            list.FormulaOperation,
            list.FormulaValue,
            list.FormulaUnit,
            list.RoundEnabled,
            list.RoundTo);
    }

    private static string SerializeList(IReadOnlyList<string> values) =>
        JsonSerializer.Serialize(values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray());

    private static IReadOnlyList<string> DeserializeList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
