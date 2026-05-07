using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class SupplierGroupRepository : ISupplierGroupRepository
{
    private readonly ApplicationDbContext _db;

    public SupplierGroupRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<SupplierGroup?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SupplierGroups.FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);

    public Task<SupplierGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim().ToLower();
        return _db.SupplierGroups.FirstOrDefaultAsync(
            g => !g.IsDeleted && g.Name.ToLower() == trimmed,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierGroupDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _db.SupplierGroups.AsNoTracking()
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Notes,
                g.CreatedDate,
                g.CreatedBy,
                SupplierCount = _db.Suppliers.Count(s => !s.IsDeleted && s.SupplierGroupId == g.Id),
            })
            .ToListAsync(cancellationToken);

        return groups
            .Select(g => new SupplierGroupDto(g.Id, g.Name, g.Notes, g.SupplierCount, g.CreatedDate, g.CreatedBy))
            .ToList();
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim().ToLower();
        return _db.SupplierGroups.AsNoTracking()
            .AnyAsync(g => !g.IsDeleted
                           && g.Name.ToLower() == trimmed
                           && (excludeId == null || g.Id != excludeId), cancellationToken);
    }

    public Task AddAsync(SupplierGroup group, CancellationToken cancellationToken = default) =>
        _db.SupplierGroups.AddAsync(group, cancellationToken).AsTask();

    public async Task<bool> SoftDeleteAsync(Guid id, string updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SupplierGroups.FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);
        if (entity is null)
            return false;
        entity.IsDeleted = true;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.UtcNow;
        return true;
    }
}
