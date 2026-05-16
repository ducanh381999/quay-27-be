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

public sealed record SalesInvoiceLineDto(
    Guid Id,
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal SellingPrice,
    decimal LineTotal,
    string? Note);

public sealed record SalesInvoiceDetailDto(
    Guid Id,
    string Code,
    string Status,
    string? CustomerCode,
    string? CustomerName,
    DateTime CreatedAtUtc,
    string? CreatedByDisplayName,
    string? SellerDisplayName,
    string? SaleChannelName,
    string? PriceListName,
    Guid? PurchaseOrderId,
    string? PurchaseOrderCode,
    string? Note,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal AmountDue,
    decimal AmountPaid,
    string BranchLabel,
    IReadOnlyList<SalesInvoiceLineDto> Lines);

public sealed record SalesInvoiceCashbookRowDto(
    Guid Id,
    string Code,
    string DisplayCode,
    DateTime OccurredAtUtc,
    string? CreatorDisplayName,
    string FundType,
    string Status,
    decimal VoucherAmount,
    decimal Amount,
    string EntryType);

public sealed record SalesInvoiceReturnRowDto(
    Guid Id,
    string Code,
    DateTime CreatedAtUtc,
    string? ReceivedByDisplayName,
    decimal Total,
    string Status);
