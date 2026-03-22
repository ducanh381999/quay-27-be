namespace Quay27.Domain.Entities;

public class Queue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<CustomerQueue> CustomerQueues { get; set; } = new List<CustomerQueue>();
}
