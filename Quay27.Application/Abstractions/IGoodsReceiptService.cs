using Quay27.Application.Purchasing;

namespace Quay27.Application.Abstractions;

public interface IGoodsReceiptService
{
    Task<ReceiptImportPreviewResult> PreviewImportAsync(byte[] fileBytes, string fileName, CancellationToken cancellationToken = default);
    Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default);
    Task<GoodsReceiptDto> UpdateAsync(Guid id, CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default);
    Task<GoodsReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoodsReceiptListItemDto>> ListAsync(ReceiptListQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoodsReceiptSupplierPaymentCashbookRowDto>?> ListSupplierPaymentCashbookEntriesAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
