namespace Quay27.Application.Cashbook;

public sealed record CashbookEntryListItemDto(
    Guid Id,
    string Code,
    string EntryType,
    string FundType,
    DateTime OccurredAtUtc,
    decimal Amount,
    string? CategoryName,
    string? CounterpartyDisplayName,
    string Status,
    bool AffectsBusinessResult,
    string SourceKind);

public sealed record CashbookSummaryDto(
    decimal OpeningBalance,
    decimal TotalReceipts,
    decimal TotalPayments,
    decimal ClosingBalance);

public sealed record CreateCashbookPartyRequest(
    string Name,
    string? Phone,
    string? Address,
    string? Province,
    string? Ward,
    string? Note);

public sealed record CashbookPartyCreatedDto(Guid Id, string Name);

public sealed record CreateCashbookReceiptRequest(
    DateTime? OccurredAtUtc,
    int PaymentCategoryId,
    Guid CollectorUserId,
    string CounterpartyScope,
    Guid? CashbookPartyId,
    string? CounterpartyDisplayName,
    decimal Amount,
    string? Note,
    bool AffectsBusinessResult,
    string FundType,
    string PartnerDebtMode,
    Guid? StaffUserId);

public sealed record CreateCashbookPaymentRequest(
    DateTime? OccurredAtUtc,
    int PaymentCategoryId,
    Guid CollectorUserId,
    string CounterpartyScope,
    Guid? CashbookPartyId,
    string? CounterpartyDisplayName,
    decimal Amount,
    string? Note,
    bool AffectsBusinessResult,
    string FundType,
    string PartnerDebtMode,
    Guid? StaffUserId);

public sealed record CashbookEntryCreatedDto(Guid Id, string Code);

public sealed record PatchOrderStatusRequest(string Status);
