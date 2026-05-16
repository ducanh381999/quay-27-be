namespace Quay27.Application.Orders;

public sealed class SalesReturnListItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? CustomerCode { get; init; }
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string ReturnType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public sealed record SalesReturnLineDto(
    Guid Id,
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal ReturnPrice,
    decimal Discount,
    decimal RestockPrice,
    decimal LineTotal,
    string? Note);

public sealed record SalesReturnDetailDto(
    Guid Id,
    string Code,
    string Status,
    string? CustomerCode,
    string? CustomerName,
    DateTime CreatedAtUtc,
    string? CreatedByDisplayName,
    string? ReceivedByDisplayName,
    Guid? ReceivedByUserId,
    string? SaleChannelName,
    string? PriceListName,
    string? Note,
    Guid? SalesInvoiceId,
    string? SalesInvoiceCode,
    Guid? PaymentVoucherId,
    string? PaymentVoucherCode,
    decimal ReturnSubtotalAmount,
    decimal ReturnDiscountAmount,
    decimal ReturnFeeAmount,
    decimal RefundDueAmount,
    decimal AmountPaid,
    string BranchLabel,
    IReadOnlyList<SalesReturnLineDto> Lines);

public sealed record SalesReturnCashbookRowDto(
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
