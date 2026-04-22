namespace Quay27.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemType { get; set; } = "goods";
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Stock { get; set; }
    public string? Barcode { get; set; }
    public Guid? GroupId { get; set; }
    public ProductGroup? Group { get; set; }
    public string? Brand { get; set; }
    public string? Location { get; set; }
    public int? MinStock { get; set; }
    public int? MaxStock { get; set; }
    public string RowStatus { get; set; } = "active";
    public bool DirectSale { get; set; }
    public string? Description { get; set; }
    public string? DescriptionRichText { get; set; }
    public string? InvoiceNoteTemplate { get; set; }
    public decimal? WeightKg { get; set; }
    public string? SupplierName { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset? ExpectedStockoutAt { get; set; }
    public int SupplierOrderQty { get; set; }
    public bool IsFavorite { get; set; }
    public bool ChannelLinked { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
