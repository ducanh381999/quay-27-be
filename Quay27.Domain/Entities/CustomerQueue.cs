namespace Quay27.Domain.Entities;

public class CustomerQueue
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public int QueueId { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public Customer Customer { get; set; } = null!;
    public Queue Queue { get; set; } = null!;
}
