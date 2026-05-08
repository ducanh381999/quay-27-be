using Microsoft.EntityFrameworkCore;
using Quay27.Application.Purchasing;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class GoodsReceiptRepository : IGoodsReceiptRepository
{
    private readonly ApplicationDbContext _db;

    public GoodsReceiptRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(GoodsReceipt entity, CancellationToken cancellationToken = default) =>
        _db.GoodsReceipts.AddAsync(entity, cancellationToken).AsTask();

    public Task<GoodsReceipt?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GoodsReceipts
            .Include(x => x.Lines)
            .Include(x => x.PaymentAllocations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<GoodsReceipt?> GetProjectedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GoodsReceipts
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Lines)
            .Include(x => x.PaymentAllocations)
            .ThenInclude(x => x.ReceivingAccount)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<GoodsReceiptListItemDto>> ListAsync(
        ReceiptListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.GoodsReceipts
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(x =>
                x.Code.Contains(search) ||
                (x.Supplier != null && (x.Supplier.Code.Contains(search) || x.Supplier.Name.Contains(search))));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            q = q.Where(x => x.Status == query.Status);
        }

        if (query.From.HasValue) q = q.Where(x => x.ReceiptDate >= query.From.Value);
        if (query.To.HasValue) q = q.Where(x => x.ReceiptDate <= query.To.Value);

        return await q
            .OrderByDescending(x => x.ReceiptDate)
            .Select(x => new GoodsReceiptListItemDto(
                x.Id,
                x.Code,
                x.ReceiptDate,
                x.Supplier != null ? x.Supplier.Code : null,
                x.Supplier != null ? x.Supplier.Name : null,
                x.SupplierDebtDelta,
                x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "PN";
        var maxCode = await _db.GoodsReceipts
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .Select(x => x.Code)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length <= prefix.Length || !int.TryParse(maxCode[prefix.Length..], out var number))
        {
            return "PN000001";
        }

        return $"{prefix}{(number + 1).ToString().PadLeft(6, '0')}";
    }
}
