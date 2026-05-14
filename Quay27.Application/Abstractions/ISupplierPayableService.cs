using Quay27.Application.Suppliers;

namespace Quay27.Application.Abstractions;

public interface ISupplierPayableService
{
    Task<IReadOnlyList<SupplierPayableTransactionDto>> ListTransactionsAsync(
        Guid supplierId,
        int? transactionType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierOpenGoodsReceiptRowDto>> ListOpenGoodsReceiptsAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task<SupplierDebtAdjustmentDto> CreateAdjustmentAsync(
        Guid supplierId,
        CreateSupplierDebtAdjustmentRequest request,
        CancellationToken cancellationToken = default);

    Task<SupplierPayablePaymentDto> CreatePaymentAsync(
        Guid supplierId,
        CreateSupplierPayablePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<SupplierPayableDiscountDto> CreateDiscountAsync(
        Guid supplierId,
        CreateSupplierPayableDiscountRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportTransactionsExcelAsync(
        Guid supplierId,
        int? transactionType,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportSupplierDebtSnapshotExcelAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default);
}
