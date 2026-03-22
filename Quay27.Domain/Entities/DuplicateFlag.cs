namespace Quay27.Domain.Entities;

public class DuplicateFlag
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public int DuplicateGroupId { get; set; }

    public Customer Customer { get; set; } = null!;
}
