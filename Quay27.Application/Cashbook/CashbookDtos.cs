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

/// <summary>One linked document row for expand + edit allocation table.</summary>
public sealed record CashbookEntryAllocationLineDto(
    Guid TargetId,
    string DocumentCode,
    DateTime DocumentAtUtc,
    decimal DocumentTotal,
    decimal PreviouslyApplied,
    decimal CurrentAmount,
    string LinkedStatus);

public sealed record CashbookEntryDetailDto(
    Guid Id,
    string Code,
    string EntryType,
    string FundType,
    DateTime OccurredAtUtc,
    decimal Amount,
    string? Note,
    int? PaymentCategoryId,
    string? PaymentCategoryName,
    string? PaymentCategoryCode,
    bool AffectsBusinessResult,
    string Status,
    Guid? CollectorUserId,
    string? CollectorFullName,
    Guid? CreatedByUserId,
    string? CreatedByFullName,
    Guid? StaffUserId,
    string? StaffFullName,
    string CounterpartyScope,
    Guid? CashbookPartyId,
    string? CounterpartyDisplayName,
    string PartnerDebtMode,
    string SourceKind,
    Guid? SourceId,
    string? SourceSummary,
    IReadOnlyList<CashbookEntryAllocationLineDto> Allocations,
    bool CanEditHeaderFields,
    bool CanEditAllocations,
    bool CanCancel);

public sealed record PatchCashbookEntryAllocationItem(Guid TargetId, decimal Amount);

public sealed record PatchCashbookEntryRequest(
    DateTime? OccurredAtUtc,
    int? PaymentCategoryId,
    Guid? CollectorUserId,
    string? CounterpartyScope,
    Guid? CashbookPartyId,
    string? CounterpartyDisplayName,
    decimal? Amount,
    string? Note,
    bool? AffectsBusinessResult,
    string? FundType,
    Guid? StaffUserId,
    IReadOnlyList<PatchCashbookEntryAllocationItem>? Allocations);

public sealed record PatchOrderStatusRequest(string Status);
