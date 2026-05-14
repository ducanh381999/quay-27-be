using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class SupplierPayableRepository : ISupplierPayableRepository
{
    private readonly ApplicationDbContext _db;

    public SupplierPayableRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SupplierPayableTransactionDto>> ListTransactionsAsync(
        Guid supplierId,
        int? transactionType,
        CancellationToken cancellationToken = default)
    {
        var list = new List<SupplierPayableTransactionDto>();

        var grs = await _db.GoodsReceipts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.SupplierId == supplierId)
            .Select(x => new { x.Id, x.Code, x.ReceiptDate, x.Total, x.SupplierDebtDelta })
            .ToListAsync(cancellationToken);

        foreach (var x in grs)
        {
            var t = SupplierPayableTransactionTypes.GoodsReceipt;
            list.Add(new SupplierPayableTransactionDto(
                x.Id,
                "goods_receipt",
                x.Code,
                x.ReceiptDate,
                t,
                SupplierPayableTransactionTypes.GetLabel(t),
                x.Total,
                x.SupplierDebtDelta));
        }

        var rrs = await _db.ReturnReceipts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.SupplierId == supplierId)
            .Select(x => new { x.Id, x.Code, x.ReturnDate, x.Total, x.SupplierDebtDelta })
            .ToListAsync(cancellationToken);

        foreach (var x in rrs)
        {
            var t = SupplierPayableTransactionTypes.SupplierReturn;
            list.Add(new SupplierPayableTransactionDto(
                x.Id,
                "return_receipt",
                x.Code,
                x.ReturnDate,
                t,
                SupplierPayableTransactionTypes.GetLabel(t),
                x.Total,
                -x.SupplierDebtDelta));
        }

        var adjs = await _db.SupplierDebtAdjustments
            .AsNoTracking()
            .Where(x => x.SupplierId == supplierId)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.OccurredAtUtc,
                x.Delta,
            })
            .ToListAsync(cancellationToken);

        foreach (var x in adjs)
        {
            var t = SupplierPayableTransactionTypes.Adjustment;
            list.Add(new SupplierPayableTransactionDto(
                x.Id,
                "adjustment",
                x.Code,
                x.OccurredAtUtc,
                t,
                SupplierPayableTransactionTypes.GetLabel(t),
                Math.Abs(x.Delta),
                x.Delta));
        }

        var pays = await _db.SupplierPayablePayments
            .AsNoTracking()
            .Where(x => x.SupplierId == supplierId)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.OccurredAtUtc,
                x.TotalAmount,
                x.PostToCashbook,
            })
            .ToListAsync(cancellationToken);

        foreach (var x in pays)
        {
            var t = x.PostToCashbook
                ? SupplierPayableTransactionTypes.CashbookPayment
                : SupplierPayableTransactionTypes.Payment;
            list.Add(new SupplierPayableTransactionDto(
                x.Id,
                "payable_payment",
                x.Code,
                x.OccurredAtUtc,
                t,
                SupplierPayableTransactionTypes.GetLabel(t),
                x.TotalAmount,
                -x.TotalAmount));
        }

        var discs = await _db.SupplierPayableDiscounts
            .AsNoTracking()
            .Where(x => x.SupplierId == supplierId)
            .Select(x => new { x.Id, x.Code, x.OccurredAtUtc, x.TotalAmount })
            .ToListAsync(cancellationToken);

        foreach (var x in discs)
        {
            var t = SupplierPayableTransactionTypes.PaymentDiscount;
            list.Add(new SupplierPayableTransactionDto(
                x.Id,
                "payable_discount",
                x.Code,
                x.OccurredAtUtc,
                t,
                SupplierPayableTransactionTypes.GetLabel(t),
                x.TotalAmount,
                -x.TotalAmount));
        }

        IEnumerable<SupplierPayableTransactionDto> q = list.OrderByDescending(x => x.OccurredAtUtc);
        if (transactionType.HasValue)
        {
            q = q.Where(x => x.TransactionType == transactionType.Value);
        }

        return q.ToList();
    }

    public async Task<IReadOnlyList<SupplierOpenGoodsReceiptRowDto>> ListOpenGoodsReceiptsAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        return await _db.GoodsReceipts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.SupplierId == supplierId && x.SupplierDebtDelta > 0.0001m)
            .OrderByDescending(x => x.ReceiptDate)
            .Select(x => new SupplierOpenGoodsReceiptRowDto(
                x.Id,
                x.Code,
                x.ReceiptDate,
                x.Total,
                x.PaidAmount,
                x.SupplierDebtDelta))
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateNextAdjustmentCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "DCC";
        var maxCode = await _db.SupplierDebtAdjustments
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .OrderByDescending(x => x.Code)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return NextFromMax(prefix, maxCode);
    }

    public async Task<string> GenerateNextPaymentCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "PCN";
        var maxCode = await _db.SupplierPayablePayments
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .OrderByDescending(x => x.Code)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return NextFromMax(prefix, maxCode);
    }

    public async Task<string> GenerateNextDiscountCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "CKP";
        var maxCode = await _db.SupplierPayableDiscounts
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .OrderByDescending(x => x.Code)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return NextFromMax(prefix, maxCode);
    }

    private static string NextFromMax(string prefix, string? maxCode)
    {
        if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length <= prefix.Length ||
            !int.TryParse(maxCode[prefix.Length..], out var number))
        {
            return $"{prefix}000001";
        }

        return $"{prefix}{(number + 1).ToString().PadLeft(6, '0')}";
    }

    public Task AddAdjustmentAsync(SupplierDebtAdjustment entity, CancellationToken cancellationToken = default) =>
        _db.SupplierDebtAdjustments.AddAsync(entity, cancellationToken).AsTask();

    public Task AddPaymentAsync(SupplierPayablePayment entity, CancellationToken cancellationToken = default) =>
        _db.SupplierPayablePayments.AddAsync(entity, cancellationToken).AsTask();

    public Task AddDiscountAsync(SupplierPayableDiscount entity, CancellationToken cancellationToken = default) =>
        _db.SupplierPayableDiscounts.AddAsync(entity, cancellationToken).AsTask();
}
