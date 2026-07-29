using Quay27.Application.Purchasing;

namespace Quay27.Application.Abstractions;

public interface IImportOrderService
{
    Task<ImportOrderDetailDto> CreateAsync(CreateImportOrderRequest request, CancellationToken cancellationToken = default);
    Task<ImportOrderDetailDto> UpdateAsync(Guid id, CreateImportOrderRequest request, CancellationToken cancellationToken = default);
    Task<ImportOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportOrderListItemDto>> ListAsync(ImportOrderListQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoodsReceiptSupplierPaymentCashbookRowDto>?> ListSupplierPaymentCashbookEntriesAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<ImportOrderSuggestResult> SuggestAsync(ImportOrderSuggestRequest request, CancellationToken cancellationToken = default);
}
