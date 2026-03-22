namespace Quay27.Domain.Entities;

public class CustomerVersion
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string SnapshotData { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public Customer Customer { get; set; } = null!;
}
