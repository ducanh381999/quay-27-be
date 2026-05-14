using Quay27.Application.Cashbook;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICashbookRepository
{
    Task<IReadOnlyList<CashbookEntryListItemDto>> ListEntriesAsync(CashbookListQuery query,
        CancellationToken cancellationToken = default);

    Task<CashbookSummaryDto> GetSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? fundType,
        IReadOnlyList<string>? entryTypes,
        int? paymentCategoryId,
        IReadOnlyList<string>? statuses,
        bool? affectsBusinessResult,
        Guid? createdByUserId,
        Guid? staffUserId,
        string? searchCode,
        string? counterpartySearch,
        string? counterpartyPhone,
        IReadOnlyList<string>? partnerDebtModes,
        CancellationToken cancellationToken = default);

    /// <summary>Next receipt code: <c>PT-{EVENT}-{NNNNNN}</c>; counter is per normalized <paramref name="eventSegment"/>.</summary>
    Task<string> GenerateNextReceiptCodeAsync(string eventSegment, CancellationToken cancellationToken = default);

    /// <summary>Next payment code: <c>PC</c> + 6 digits (global sequence for payments).</summary>
    Task<string> GenerateNextPaymentCodeAsync(CancellationToken cancellationToken = default);
    Task AddEntryAsync(CashbookEntry entity, CancellationToken cancellationToken = default);
    Task AddPartyAsync(CashbookParty entity, CancellationToken cancellationToken = default);
    Task<CashbookParty?> GetPartyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsEntryForSourceAsync(string sourceKind, Guid sourceId, CancellationToken cancellationToken = default);
    Task RemoveEntriesBySourceAsync(string sourceKind, Guid sourceId, CancellationToken cancellationToken = default);

    Task<CashbookEntry?> GetEntryForReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CashbookEntry?> GetEntryTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SupplierPaymentAllocation?> GetSupplierPaymentAllocationTrackedAsync(Guid allocationId,
        CancellationToken cancellationToken = default);

    Task<SupplierPaymentAllocation?> GetSupplierPaymentAllocationForReadAsync(Guid allocationId,
        CancellationToken cancellationToken = default);
}
