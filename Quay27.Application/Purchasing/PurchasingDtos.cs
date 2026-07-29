namespace Quay27.Application.Purchasing;

public sealed record ReceiptLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    string? Unit = null,
    string? Note = null);

public sealed record ReturnReceiptLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal ImportPrice,
    decimal ReturnPrice,
    decimal Discount,
    string? Unit = null,
    string? Note = null);

public sealed record PaymentAllocationInput(
    string PaymentMethod,
    decimal Amount,
    Guid? ReceivingAccountId);

public sealed record CreateGoodsReceiptRequest(
    Guid? SupplierId,
    string? Status,
    DateTime? ReceiptDate,
    decimal Discount,
    decimal PaidAmount,
    string? Notes,
    IReadOnlyList<ReceiptLineInput> Lines,
    IReadOnlyList<PaymentAllocationInput>? PaymentAllocations,
    Guid? ImportOrderId = null);

public sealed record CreateReturnReceiptRequest(
    Guid? SupplierId,
    string? Status,
    DateTime? ReturnDate,
    decimal Discount,
    decimal SupplierPaidAmount,
    string? Notes,
    IReadOnlyList<ReturnReceiptLineInput> Lines,
    IReadOnlyList<PaymentAllocationInput>? PaymentAllocations);

public sealed record ReceiptLineDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal,
    string? Note);

public sealed record ReturnReceiptLineDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    decimal Quantity,
    decimal ImportPrice,
    decimal ReturnPrice,
    decimal Discount,
    decimal LineTotal,
    string? Note);

public sealed record PaymentAllocationDto(
    Guid Id,
    string PaymentMethod,
    decimal Amount,
    Guid? ReceivingAccountId,
    string? ReceivingAccountName);

public sealed record GoodsReceiptSupplierPaymentCashbookRowDto(
    Guid Id,
    string Code,
    DateTime OccurredAtUtc,
    string? CreatorDisplayName,
    string FundType,
    string Status,
    decimal Amount);

public sealed record ReturnReceiptSupplierRefundCashbookRowDto(
    Guid Id,
    string Code,
    DateTime OccurredAtUtc,
    string? CreatorDisplayName,
    string FundType,
    string Status,
    decimal Amount);

public sealed record GoodsReceiptDto(
    Guid Id,
    string Code,
    Guid? SupplierId,
    string? SupplierCode,
    string? SupplierName,
    string Status,
    DateTime ReceiptDate,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    decimal PaidAmount,
    decimal SupplierDebtDelta,
    string Notes,
    string CreatedBy,
    IReadOnlyList<ReceiptLineDto> Lines,
    IReadOnlyList<PaymentAllocationDto> PaymentAllocations);

public sealed record ReturnReceiptDto(
    Guid Id,
    string Code,
    Guid? SupplierId,
    string? SupplierCode,
    string? SupplierName,
    string Status,
    DateTime ReturnDate,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    decimal SupplierPaidAmount,
    decimal SupplierDebtDelta,
    string Notes,
    string CreatedBy,
    IReadOnlyList<ReturnReceiptLineDto> Lines,
    IReadOnlyList<PaymentAllocationDto> PaymentAllocations);

public sealed record GoodsReceiptListItemDto(
    Guid Id,
    string Code,
    DateTime ReceiptDate,
    string? SupplierCode,
    string? SupplierName,
    decimal SupplierDebtDelta,
    string Status);

public sealed record ReturnReceiptListItemDto(
    Guid Id,
    string Code,
    DateTime ReturnDate,
    string? SupplierName,
    decimal Subtotal,
    decimal Discount,
    decimal SupplierDebtDelta,
    decimal SupplierPaidAmount,
    string Status);

public sealed record ReceiptListQuery(
    string? Search,
    IReadOnlyList<string>? Statuses,
    DateTime? From,
    DateTime? To,
    Guid? SupplierId = null,
    bool OutstandingDebtOnly = false);

public sealed record ReceiptImportPreviewItem(
    int RowNumber,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal);

public sealed record ReceiptImportPreviewResult(
    int TotalRows,
    int ValidRows,
    int FailedRows,
    IReadOnlyList<string> Errors,
    IReadOnlyList<ReceiptImportPreviewItem> Items);

public sealed record ReceivingAccountDto(
    Guid Id,
    string Name,
    string AccountNumber,
    string BankName,
    bool IsActive,
    string? ProviderCode);

public sealed record ImportOrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    decimal Quantity,
    decimal QuantityReceived,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal,
    string? Note);

public sealed record ImportOrderListItemDto(
    Guid Id,
    string Code,
    DateTime CreatedAtUtc,
    Guid? SupplierId,
    string? SupplierName,
    DateTime? ExpectedReceiptDate,
    int? WaitingDays,
    decimal AmountDue,
    string Status,
    string BranchLabel);

public sealed record ImportOrderDetailDto(
    Guid Id,
    string Code,
    string Status,
    Guid? SupplierId,
    string? SupplierCode,
    string? SupplierName,
    DateTime CreatedAtUtc,
    string? CreatedByDisplayName,
    string? OrderedByDisplayName,
    DateTime? ExpectedReceiptDate,
    string? Note,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal AmountDue,
    decimal AmountPaid,
    string BranchLabel,
    IReadOnlyList<ImportOrderLineDto> Lines);

public sealed record CreateImportOrderRequest(
    Guid? SupplierId,
    string? Status,
    DateTime? ExpectedReceiptDate,
    decimal Discount,
    decimal PaidAmount,
    string? Notes,
    IReadOnlyList<ReceiptLineInput> Lines);

public sealed record ImportOrderListQuery(
    string? Search,
    IReadOnlyList<string>? Statuses,
    DateTime? From,
    DateTime? To,
    string? CreatedBy = null,
    string? OrderedBy = null);

public sealed record ImportOrderSuggestRequest(
    Guid? SupplierId,
    Guid? GroupId,
    string StockMode,
    string QtyMode,
    int SoldDays = 30);

public sealed record ImportOrderSuggestLineDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    decimal Quantity,
    decimal UnitPrice);

public sealed record ImportOrderSuggestResult(
    IReadOnlyList<ImportOrderSuggestLineDto> Lines);
