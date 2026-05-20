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

public sealed record PurchaseOrderLineDto(
    Guid Id,
    string ProductCode,
    string ProductName,
    int QuantityOrdered,
    int QuantityFulfilled,
    decimal UnitPrice,
    decimal Discount,
    decimal SellingPrice,
    decimal LineTotal,
    string? Note);

public sealed record PurchaseOrderDetailDto(
    Guid Id,
    string Code,
    string Status,
    string? CustomerCode,
    string? CustomerName,
    DateTime CreatedAtUtc,
    string? CreatedByDisplayName,
    string? ReceivedByDisplayName,
    string? SellerDisplayName,
    string? SaleChannelName,
    string? PriceListName,
    Guid? PrimaryInvoiceId,
    string? PrimaryInvoiceCode,
    string? ExpectedDelivery,
    string? Note,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal AmountDue,
    decimal AmountPaid,
    string BranchLabel,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record PurchaseOrderLinkedInvoiceDto(
    Guid Id,
    string Code,
    DateTime CreatedAtUtc,
    string? CreatorDisplayName,
    decimal Value,
    string Status);

public sealed record PurchaseOrderCashbookRowDto(
    Guid Id,
    string Code,
    DateTime OccurredAtUtc,
    string? CreatorDisplayName,
    string FundType,
    string Status,
    decimal Amount,
    string EntryType);
