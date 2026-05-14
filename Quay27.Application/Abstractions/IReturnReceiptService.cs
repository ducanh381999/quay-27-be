using Quay27.Application.Purchasing;

namespace Quay27.Application.Abstractions;

public interface IReturnReceiptService
{
    Task<ReceiptImportPreviewResult> PreviewImportAsync(byte[] fileBytes, string fileName, CancellationToken cancellationToken = default);
    Task<ReturnReceiptDto> CreateAsync(CreateReturnReceiptRequest request, CancellationToken cancellationToken = default);
    Task<ReturnReceiptDto> UpdateAsync(Guid id, CreateReturnReceiptRequest request, CancellationToken cancellationToken = default);
    Task<ReturnReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnReceiptListItemDto>> ListAsync(ReceiptListQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnReceiptSupplierRefundCashbookRowDto>?> ListSupplierRefundCashbookEntriesAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
