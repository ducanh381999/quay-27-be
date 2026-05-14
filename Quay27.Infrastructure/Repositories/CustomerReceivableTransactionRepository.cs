using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class CustomerReceivableTransactionRepository : ICustomerReceivableTransactionRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerReceivableTransactionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<CustomerReceivableTransaction> Items, int TotalCount)> ListPagedAsync(
        Guid customerProfileId,
        int skip,
        int take,
        string? kind,
        CancellationToken cancellationToken = default)
    {
        var q = _db.CustomerReceivableTransactions.AsNoTracking()
            .Where(x => x.CustomerProfileId == customerProfileId);
        if (!string.IsNullOrWhiteSpace(kind) && kind.Trim().ToLowerInvariant() != "all")
        {
            var k = kind.Trim().ToLowerInvariant();
            q = q.Where(x => x.Kind == k);
        }

        var total = await q.CountAsync(cancellationToken);
        var takeClamped = Math.Clamp(take, 1, 200);
        var skipClamped = Math.Max(0, skip);
        var items = await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skipClamped)
            .Take(takeClamped)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task AddAsync(CustomerReceivableTransaction entity, CancellationToken cancellationToken = default) =>
        _db.CustomerReceivableTransactions.AddAsync(entity, cancellationToken).AsTask();
}
