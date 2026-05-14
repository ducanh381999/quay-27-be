using Quay27.Application.Cashbook;

namespace Quay27.Application.Abstractions;

public interface ICashbookService
{
    Task<IReadOnlyList<CashbookEntryListItemDto>> ListEntriesAsync(CashbookListQuery query,
        CancellationToken cancellationToken = default);

    Task<CashbookSummaryDto> GetSummaryAsync(CashbookListQuery query, CancellationToken cancellationToken = default);

    Task<CashbookPartyCreatedDto> CreatePartyAsync(CreateCashbookPartyRequest request,
        CancellationToken cancellationToken = default);

    Task<CashbookEntryCreatedDto> CreateReceiptAsync(CreateCashbookReceiptRequest request,
        CancellationToken cancellationToken = default);

    Task<CashbookEntryCreatedDto> CreatePaymentAsync(CreateCashbookPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<CashbookEntryDetailDto> GetEntryDetailAsync(Guid id, CancellationToken cancellationToken = default);

    Task PatchEntryAsync(Guid id, PatchCashbookEntryRequest request, CancellationToken cancellationToken = default);

    Task CancelEntryAsync(Guid id, CancellationToken cancellationToken = default);
}
