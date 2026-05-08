using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class ReceivingAccountRepository : IReceivingAccountRepository
{
    private readonly ApplicationDbContext _db;

    public ReceivingAccountRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReceivingAccount>> ListActiveAsync(CancellationToken cancellationToken = default)
        => await _db.ReceivingAccounts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<ReceivingAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.ReceivingAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
}
