using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Products;
using Quay27.Application.Repositories;
using ClosedXML.Excel;
using Quay27.Domain.Entities;
using System.Text.Json;

namespace Quay27.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IProductGroupRepository _groups;
    private readonly IPriceListRepository _priceLists;
    private readonly IPriceListItemRepository _priceListItems;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository products,
        IProductGroupRepository groups,
        IPriceListRepository priceLists,
        IPriceListItemRepository priceListItems,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _products = products;
        _groups = groups;
        _priceLists = priceLists;
        _priceListItems = priceListItems;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductListResponse> ListAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var (items, total) = await _products.ListAsync(query, cancellationToken);
        return new ProductListResponse { Items = items.Select(x => Map(x)).ToList(), Total = total };
    }

    public async Task<ProductListItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _products.GetByIdAsync(id, cancellationToken);
        if (item is null) throw new NotFoundException("Product not found.");
        return Map(item);
    }

    public async Task<ProductListItemDto> CreateAsync(UpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity, group);
    }

    public async Task<ProductListItemDto> UpdateAsync(Guid id, UpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        return ProductGroupTreeBuilder.Build(groups);
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

        var items = await _priceListItems.ListByPriceListIdsAsync(
            query.PriceListIds,
            query.Search,
            query.GroupId,
            query.Stock,
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

    public async Task<byte[]?> ExportPriceListAsync(
        PriceListItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        EnsureCanManagePriceListActions();

        var items = await ListPriceListItemsAsync(query, cancellationToken);
        if (items.Count == 0) return null;

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("PriceListExport");
        ws.Cell(1, 1).Value = "Mã hàng";
        ws.Cell(1, 2).Value = "Tên hàng";
        ws.Cell(1, 3).Value = "Giá vốn";
        ws.Cell(1, 4).Value = "Giá nhập cuối";
        ws.Cell(1, 5).Value = "Giá theo bảng";

        for (var i = 0; i < items.Count; i++)
        {
            var row = i + 2;
            var item = items[i];
            ws.Cell(row, 1).Value = item.ProductCode;
            ws.Cell(row, 2).Value = item.ProductName;
            ws.Cell(row, 3).Value = item.CostPrice;
            ws.Cell(row, 4).Value = item.LastImportPrice;
            ws.Cell(row, 5).Value = item.PricesByListId.Values.FirstOrDefault();
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
            var key = cell.GetString().Trim().Replace(" ", string.Empty).ToLowerInvariant();
            if (key is "mãhàng" or "mahang")
                map["productcode"] = cell.Address.ColumnNumber;
            else if (key is "tênbảnggiá" or "tenbanggia" or "banggia")
                map["pricelistname"] = cell.Address.ColumnNumber;
            else if (key is "giá" or "gia" or "price")
                map["price"] = cell.Address.ColumnNumber;
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

        var existing = await _groups.GetByNameAsync(value, cancellationToken);
        if (existing is not null) return existing;

        var created = new ProductGroup
        {
            Id = Guid.NewGuid(),
            Name = value,
            CreatedDate = DateTime.UtcNow
        };
        await _groups.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static ProductListItemDto Map(Product x, ProductGroup? g = null)
    {
        var group = g ?? x.Group;
        return new ProductListItemDto
        {
            Id = x.Id,
            ImageUrl = x.ImageUrl,
            Code = x.Code,
            Name = x.Name,
            ItemType = x.ItemType,
            SalePrice = x.SalePrice,
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
