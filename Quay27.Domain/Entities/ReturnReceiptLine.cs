namespace Quay27.Domain.Entities;

public class ReturnReceiptLine
{
    public Guid Id { get; set; }
    public Guid ReturnReceiptId { get; set; }
    public ReturnReceipt ReturnReceipt { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductCodeSnapshot { get; set; } = string.Empty;
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string UnitSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ImportPrice { get; set; }
    public decimal ReturnPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
}
