using Microsoft.EntityFrameworkCore;
using Quay27.Application.Purchasing;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class ReturnReceiptRepository : IReturnReceiptRepository
{
    private readonly ApplicationDbContext _db;

    public ReturnReceiptRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(ReturnReceipt entity, CancellationToken cancellationToken = default) =>
        _db.ReturnReceipts.AddAsync(entity, cancellationToken).AsTask();

    public Task<ReturnReceipt?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ReturnReceipts
            .Include(x => x.Lines)
            .Include(x => x.PaymentAllocations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<ReturnReceipt?> GetProjectedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ReturnReceipts
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Lines)
            .Include(x => x.PaymentAllocations)
            .ThenInclude(x => x.ReceivingAccount)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<ReturnReceiptListItemDto>> ListAsync(
        ReceiptListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.ReturnReceipts
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(x =>
                x.Code.Contains(search) ||
                (x.Supplier != null && x.Supplier.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            q = q.Where(x => x.Status == query.Status);
        }

        if (query.From.HasValue) q = q.Where(x => x.ReturnDate >= query.From.Value);
        if (query.To.HasValue) q = q.Where(x => x.ReturnDate <= query.To.Value);
        if (query.SupplierId.HasValue) q = q.Where(x => x.SupplierId == query.SupplierId.Value);
        if (query.OutstandingDebtOnly) q = q.Where(x => x.SupplierDebtDelta > 0.0001m);

        return await q
            .OrderByDescending(x => x.ReturnDate)
            .Select(x => new ReturnReceiptListItemDto(
                x.Id,
                x.Code,
                x.ReturnDate,
                x.Supplier != null ? x.Supplier.Name : null,
                x.Subtotal,
                x.Discount,
                x.SupplierDebtDelta,
                x.SupplierPaidAmount,
                x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "THN";
        var maxCode = await _db.ReturnReceipts
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .Select(x => x.Code)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length <= prefix.Length || !int.TryParse(maxCode[prefix.Length..], out var number))
        {
            return "THN000001";
        }

        return $"{prefix}{(number + 1).ToString().PadLeft(6, '0')}";
    }
}
