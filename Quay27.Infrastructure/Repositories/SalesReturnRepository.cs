using Microsoft.EntityFrameworkCore;
using Quay27.Application.Orders;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class SalesReturnRepository : ISalesReturnRepository
{
    private readonly ApplicationDbContext _db;

    public SalesReturnRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalesReturnListItemDto>> ListAsync(SalesReturnListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.SalesReturns.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.Code.Contains(s) ||
                (x.CustomerCode != null && x.CustomerCode.Contains(s)) ||
                (x.CustomerName != null && x.CustomerName.Contains(s)));
        }

        if (query.ReturnTypes is { Count: > 0 })
            q = q.Where(x => query.ReturnTypes.Contains(x.ReturnType));
        if (query.Statuses is { Count: > 0 })
            q = q.Where(x => query.Statuses.Contains(x.Status));

        if (query.FromUtc.HasValue) q = q.Where(x => x.CreatedAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue) q = q.Where(x => x.CreatedAtUtc <= query.ToUtc.Value);

        if (query.CreatedByUserIds is { Count: > 0 })
            q = q.Where(x => x.CreatedByUserId.HasValue && query.CreatedByUserIds.Contains(x.CreatedByUserId.Value));
        if (query.ReceivedByUserIds is { Count: > 0 })
            q = q.Where(x => x.ReceivedByUserId.HasValue && query.ReceivedByUserIds.Contains(x.ReceivedByUserId.Value));
        if (query.SaleChannelIds is { Count: > 0 })
            q = q.Where(x => x.SaleChannelId.HasValue && query.SaleChannelIds.Contains(x.SaleChannelId.Value));
        if (query.OtherCollectionTypes is { Count: > 0 })
            q = q.Where(x => x.OtherCollectionType != null && query.OtherCollectionTypes.Contains(x.OtherCollectionType));

        return await q.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new SalesReturnListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                CreatedAtUtc = x.CreatedAtUtc,
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                Status = x.Status,
                ReturnType = x.ReturnType,
                Amount = x.Amount,
            })
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(SalesReturn entity, CancellationToken cancellationToken = default) =>
        _db.SalesReturns.AddAsync(entity, cancellationToken).AsTask();

    public Task<SalesReturn?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SalesReturns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "TH";
        var maxCode = await _db.SalesReturns
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .Select(x => x.Code)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length <= prefix.Length ||
            !int.TryParse(maxCode[prefix.Length..], out var number))
            return $"{prefix}000001";

        return $"{prefix}{(number + 1).ToString().PadLeft(6, '0')}";
    }
}
