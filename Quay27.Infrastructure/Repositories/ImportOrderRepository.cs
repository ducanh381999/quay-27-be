using Microsoft.EntityFrameworkCore;
using Quay27.Application.Purchasing;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class ImportOrderRepository : IImportOrderRepository
{
    private readonly ApplicationDbContext _db;

    public ImportOrderRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(ImportOrder entity, CancellationToken cancellationToken = default) =>
        _db.ImportOrders.AddAsync(entity, cancellationToken).AsTask();

    public Task<ImportOrder?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ImportOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<ImportOrder?> GetProjectedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ImportOrders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<ImportOrderListItemDto>> ListAsync(
        ImportOrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.ImportOrders
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

        if (query.Statuses is { Count: > 0 })
        {
            q = q.Where(x => query.Statuses.Contains(x.Status));
        }

        if (query.From.HasValue) q = q.Where(x => x.CreatedDate >= query.From.Value);
        if (query.To.HasValue) q = q.Where(x => x.CreatedDate <= query.To.Value);

        if (!string.IsNullOrWhiteSpace(query.CreatedBy))
        {
            var createdBy = query.CreatedBy.Trim();
            q = q.Where(x => x.CreatedBy == createdBy);
        }

        if (!string.IsNullOrWhiteSpace(query.OrderedBy))
        {
            var orderedBy = query.OrderedBy.Trim();
            q = q.Where(x => x.OrderedBy == orderedBy);
        }

        var rows = await q
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.CreatedDate,
                x.SupplierId,
                SupplierName = x.Supplier != null ? x.Supplier.Name : null,
                x.ExpectedReceiptDate,
                x.Total,
                x.Status,
            })
            .ToListAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        return rows.Select(x =>
        {
            int? waitingDays = null;
            if (x.ExpectedReceiptDate.HasValue)
            {
                var days = (int)(today - x.ExpectedReceiptDate.Value.Date).TotalDays;
                waitingDays = Math.Max(0, days);
            }

            return new ImportOrderListItemDto(
                x.Id,
                x.Code,
                x.CreatedDate,
                x.SupplierId,
                x.SupplierName,
                x.ExpectedReceiptDate,
                waitingDays,
                x.Total,
                x.Status,
                "LK3");
        }).ToList();
    }

    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "PDN";
        var maxCode = await _db.ImportOrders
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .Select(x => x.Code)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length <= prefix.Length ||
            !int.TryParse(maxCode[prefix.Length..], out var number))
        {
            return "PDN000001";
        }

        return $"{prefix}{(number + 1).ToString().PadLeft(6, '0')}";
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ImportOrders.AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<decimal?> GetLastOrderedQuantityAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _db.ImportOrderLines
            .AsNoTracking()
            .Where(l => l.ProductId == productId && !l.ImportOrder.IsDeleted)
            .OrderByDescending(l => l.ImportOrder.CreatedDate)
            .Select(l => (decimal?)l.Quantity)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
