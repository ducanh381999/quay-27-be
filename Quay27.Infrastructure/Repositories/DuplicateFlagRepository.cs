using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class DuplicateFlagRepository : IDuplicateFlagRepository
{
    private readonly ApplicationDbContext _db;

    public DuplicateFlagRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task ReplaceFlagsForCustomersAsync(IReadOnlyList<Guid> customerIds, int duplicateGroupId, CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
            return;

        var existing = await _db.DuplicateFlags
            .Where(f => customerIds.Contains(f.CustomerId))
            .ToListAsync(cancellationToken);
        _db.DuplicateFlags.RemoveRange(existing);

        foreach (var id in customerIds)
        {
            await _db.DuplicateFlags.AddAsync(new DuplicateFlag
            {
                Id = Guid.NewGuid(),
                CustomerId = id,
                DuplicateGroupId = duplicateGroupId
            }, cancellationToken);
        }
    }

    public async Task ClearFlagsForCustomersAsync(IReadOnlyList<Guid> customerIds, CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
            return;
        var existing = await _db.DuplicateFlags
            .Where(f => customerIds.Contains(f.CustomerId))
            .ToListAsync(cancellationToken);
        _db.DuplicateFlags.RemoveRange(existing);
    }
}
