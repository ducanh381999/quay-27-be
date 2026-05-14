namespace Quay27.Application.CustomerProfiles;

public sealed record CustomerReceivableTransactionDto(
    Guid Id,
    string Kind,
    DateTime OccurredAtUtc,
    decimal Amount,
    decimal BalanceAfter,
    string? PaymentMethod,
    string? CollectorOrPerformerName,
    string? Note,
    string? Description,
    Guid? ReceivingAccountId,
    bool AllocateToInvoice,
    DateTime CreatedAtUtc,
    string CreatedBy);

public sealed record PagedReceivableTransactionsResult(
    IReadOnlyList<CustomerReceivableTransactionDto> Items,
    int TotalCount);

public sealed record RecordCustomerPaymentRequest(
    DateTime OccurredAtUtc,
    string CollectorName,
    string PaymentMethod,
    decimal Amount,
    string? Note,
    bool AllocateToInvoice);

public sealed record RecordCustomerAdjustmentRequest(
    DateTime OccurredAtUtc,
    decimal NewDebtAbsolute,
    string? Description);

public sealed record RecordCustomerPaymentDiscountRequest(
    DateTime OccurredAtUtc,
    string PerformerName,
    decimal DiscountAmount,
    string? Note,
    bool AllocateToInvoice);

public sealed record RecordCustomerQrPaymentRequest(
    Guid ReceivingAccountId,
    decimal Amount,
    string? Note);
