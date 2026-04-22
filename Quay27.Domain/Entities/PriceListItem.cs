namespace Quay27.Domain.Entities;

public class PriceListItem
{
    public Guid Id { get; set; }
    public Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Price { get; set; }
    public bool AppliedByFormula { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
