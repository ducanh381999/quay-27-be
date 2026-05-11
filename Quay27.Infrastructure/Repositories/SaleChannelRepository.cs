using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class SaleChannelRepository : ISaleChannelRepository
{
    private readonly ApplicationDbContext _db;

    public SaleChannelRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SaleChannel>> ListActiveOrderedAsync(CancellationToken cancellationToken = default) =>
        await _db.SaleChannels
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SaleChannel>> ListAllOrderedAsync(CancellationToken cancellationToken = default) =>
        await _db.SaleChannels
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var q = _db.SaleChannels.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            q = q.Where(x => x.Id != excludeId.Value);

        return q.AnyAsync(cancellationToken);
    }

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SaleChannels.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(SaleChannel entity, CancellationToken cancellationToken = default) =>
        _db.SaleChannels.AddAsync(entity, cancellationToken).AsTask();
}
