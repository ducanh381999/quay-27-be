namespace Quay27.Domain.Entities;

public class SupplierGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
