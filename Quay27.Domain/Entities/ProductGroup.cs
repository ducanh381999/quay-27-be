namespace Quay27.Domain.Entities;

public class ProductGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public ProductGroup? Parent { get; set; }
    public ICollection<ProductGroup> Children { get; set; } = new List<ProductGroup>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
