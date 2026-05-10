namespace Quay27.Domain.Entities;

public sealed class SalesReturnExchangeItem
{
    public Guid Id { get; set; }
    public Guid SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
