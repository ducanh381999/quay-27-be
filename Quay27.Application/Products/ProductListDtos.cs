namespace Quay27.Application.Products;

public sealed class ProductListItemDto
{
    public Guid Id { get; set; }
    public string? ImageUrl { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ItemType { get; set; } = "goods";
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Stock { get; set; }
    public int CustomerOrders { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpectedStockoutAt { get; set; }
    public int SupplierOrderQty { get; set; }
    public bool IsFavorite { get; set; }

    public string? Barcode { get; set; }
    public string? GroupName { get; set; }
    public string? GroupId { get; set; }
    public string? ProductType { get; set; }
    public bool ChannelLinked { get; set; }
    public string? Brand { get; set; }
    public string? Location { get; set; }
    public int? MinStock { get; set; }
    public int? MaxStock { get; set; }
    public string? RowStatus { get; set; }

    public bool DirectSale { get; set; }
    public string? Description { get; set; }
    public string? Note { get; set; }
    public string? InvoiceNoteTemplate { get; set; }
    public string? DescriptionRichText { get; set; }
    public decimal? WeightKg { get; set; }
    public string? SupplierName { get; set; }
    public IReadOnlyList<ProductComboComponentDto> ComboComponents { get; set; } = Array.Empty<ProductComboComponentDto>();
}

public sealed class ProductListResponse
{
    public IReadOnlyList<ProductListItemDto> Items { get; set; } = Array.Empty<ProductListItemDto>();
    public int Total { get; set; }
}

public sealed class ProductComboComponentDto
{
    public Guid ProductId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
}

public sealed class UpsertProductRequest
{
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public string ItemType { get; set; } = "goods";
    public string? Barcode { get; set; }
    public string? GroupName { get; set; }
    public string? Brand { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public string? Location { get; set; }
    public decimal WeightValue { get; set; }
    public string WeightUnit { get; set; } = "g";
    public string? Description { get; set; }
    public string? DescriptionRichText { get; set; }
    public string? InvoiceNoteTemplate { get; set; }
    public bool DirectSale { get; set; }
    public IReadOnlyList<string> Images { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ProductComboComponentDto> ComboComponents { get; set; } = Array.Empty<ProductComboComponentDto>();
}

public sealed class UpdateProductStatusRequest
{
    public string Status { get; set; } = "active";
}

public sealed class UpdateProductGroupRequest
{
    public string GroupName { get; set; } = "";
}

public sealed class ProductQuery
{
    public string? Search { get; set; }
    public string? GroupId { get; set; }
    public string? Stock { get; set; }
    public string? DirectSale { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
    public DateTimeOffset? ExpectedFrom { get; set; }
    public DateTimeOffset? ExpectedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

public sealed class ProductGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid? ParentId { get; set; }
}

public sealed class ProductGroupTreeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public IReadOnlyList<ProductGroupTreeDto> Children { get; set; } = Array.Empty<ProductGroupTreeDto>();
}

public sealed class CreateProductGroupRequest
{
    public string Name { get; set; } = "";
    public Guid? ParentId { get; set; }
}
