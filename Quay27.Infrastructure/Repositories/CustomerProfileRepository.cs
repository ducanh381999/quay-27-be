using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerProfileRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerProfile>> ListAsync(string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CustomerProfiles.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim();
            query = query.Where(x => x.CustomerName.Contains(t) || x.CustomerCode.Contains(t) || x.Phone1.Contains(t));
        }

        return await query
            .OrderByDescending(x => x.CreatedDate)
            .ThenBy(x => x.CustomerName)
            .ToListAsync(cancellationToken);
    }

    public Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<CustomerProfile?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<bool> CustomerCodeExistsAsync(string customerCode, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.AnyAsync(x => x.CustomerCode == customerCode, cancellationToken);

    public Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.AddAsync(profile, cancellationToken).AsTask();
}
