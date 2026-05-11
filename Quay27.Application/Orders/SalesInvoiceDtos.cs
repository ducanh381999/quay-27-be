namespace Quay27.Application.Orders;

public sealed class SalesInvoiceListItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? ReturnReferenceCode { get; init; }
    public string? CustomerCode { get; init; }
    public string? CustomerName { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
}
