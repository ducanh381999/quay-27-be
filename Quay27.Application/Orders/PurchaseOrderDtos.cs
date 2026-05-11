namespace Quay27.Application.Orders;

public sealed class PurchaseOrderListItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? CustomerCode { get; init; }
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal AmountDue { get; init; }
    public decimal AmountPaid { get; init; }
}
