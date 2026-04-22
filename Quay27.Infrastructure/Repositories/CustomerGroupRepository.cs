using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class CustomerGroupRepository : ICustomerGroupRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerGroupRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerGroup>> ListAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CustomerGroups.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term));
        }

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<CustomerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.CustomerGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<CustomerGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.CustomerGroups.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.CustomerGroups.AnyAsync(
            x => !x.IsDeleted && x.Name == name && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task AddAsync(CustomerGroup group, CancellationToken cancellationToken = default) =>
        _db.CustomerGroups.AddAsync(group, cancellationToken).AsTask();
}
