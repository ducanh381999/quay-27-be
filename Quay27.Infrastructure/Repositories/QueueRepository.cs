using Microsoft.EntityFrameworkCore;
using Quay27.Application.Queues;
using Quay27.Application.Repositories;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class QueueRepository : IQueueRepository
{
    private readonly ApplicationDbContext _db;

    public QueueRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<QueueDto>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Queues.AsNoTracking()
            .Where(q => q.IsActive)
            .OrderBy(q => q.Id)
            .Select(q => new QueueDto(q.Id, q.Name, q.IsActive))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int queueId, CancellationToken cancellationToken = default) =>
        _db.Queues.AsNoTracking().AnyAsync(q => q.Id == queueId && q.IsActive, cancellationToken);
}
