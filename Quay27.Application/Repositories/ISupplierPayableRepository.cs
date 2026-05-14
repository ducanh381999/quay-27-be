using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ISupplierPayableRepository
{
    Task<IReadOnlyList<SupplierPayableTransactionDto>> ListTransactionsAsync(
        Guid supplierId,
        int? transactionType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierOpenGoodsReceiptRowDto>> ListOpenGoodsReceiptsAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task<string> GenerateNextAdjustmentCodeAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateNextPaymentCodeAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateNextDiscountCodeAsync(CancellationToken cancellationToken = default);

    Task AddAdjustmentAsync(SupplierDebtAdjustment entity, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(SupplierPayablePayment entity, CancellationToken cancellationToken = default);
    Task AddDiscountAsync(SupplierPayableDiscount entity, CancellationToken cancellationToken = default);
}
