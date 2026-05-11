namespace Quay27.Application.Cashbook;

public sealed record CashbookListQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? FundType,
    IReadOnlyList<string>? EntryTypes,
    int? PaymentCategoryId,
    IReadOnlyList<string>? Statuses,
    bool? AffectsBusinessResult,
    Guid? CreatedByUserId,
    Guid? StaffUserId,
    string? SearchCode,
    string? CounterpartySearch,
    string? CounterpartyPhone,
    IReadOnlyList<string>? PartnerDebtModes,
    int Page,
    int PageSize);
