using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _db;

    public SupplierRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Supplier?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

    public Task<Supplier?> GetTrackedByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.Suppliers.FirstOrDefaultAsync(s => s.Code == code && !s.IsDeleted, cancellationToken);

    public async Task<SupplierDto?> GetProjectedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.Suppliers.AsNoTracking()
            .Include(s => s.SupplierGroup)
            .Where(s => s.Id == id && !s.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<SupplierDto>> ListAsync(ListSuppliersQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.Suppliers.AsNoTracking()
            .Include(s => s.SupplierGroup)
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x => x.Code.Contains(s) || x.Name.Contains(s) || x.Phone.Contains(s));
        }

        if (query.GroupId is { } gid)
            q = q.Where(x => x.SupplierGroupId == gid);

        if (string.Equals(query.Status, "active", StringComparison.OrdinalIgnoreCase))
            q = q.Where(x => x.IsActive);
        else if (string.Equals(query.Status, "inactive", StringComparison.OrdinalIgnoreCase))
            q = q.Where(x => !x.IsActive);

        if (query.TotalPurchaseMin is { } tmin)
            q = q.Where(x => x.TotalPurchase >= tmin);
        if (query.TotalPurchaseMax is { } tmax)
            q = q.Where(x => x.TotalPurchase <= tmax);

        if (query.CurrentDebtMin is { } dmin)
            q = q.Where(x => x.CurrentDebt >= dmin);
        if (query.CurrentDebtMax is { } dmax)
            q = q.Where(x => x.CurrentDebt <= dmax);

        if (query.CreatedFrom is { } cf)
        {
            var dt = cf.ToDateTime(TimeOnly.MinValue);
            q = q.Where(x => x.CreatedDate >= dt);
        }
        if (query.CreatedTo is { } ct)
        {
            var dt = ct.ToDateTime(TimeOnly.MaxValue);
            q = q.Where(x => x.CreatedDate <= dt);
        }

        var rows = await q
            .OrderByDescending(x => x.CreatedDate)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _db.Suppliers.AsNoTracking()
            .AnyAsync(s => !s.IsDeleted
                           && s.Code == code
                           && (excludeId == null || s.Id != excludeId), cancellationToken);

    public async Task<string?> GetMaxNumericCodeSuffixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var pattern = $"{prefix}%";
        var codes = await _db.Suppliers.AsNoTracking()
            .Where(s => EF.Functions.Like(s.Code, pattern))
            .Select(s => s.Code)
            .ToListAsync(cancellationToken);
        var max = 0;
        foreach (var c in codes)
        {
            var suffix = c.Substring(prefix.Length);
            if (int.TryParse(suffix, out var n) && n > max) max = n;
        }
        return max == 0 ? null : max.ToString();
    }

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
        _db.Suppliers.AddAsync(supplier, cancellationToken).AsTask();

    public async Task<bool> SoftDeleteAsync(Guid id, string updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (entity is null)
            return false;
        entity.IsDeleted = true;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.UtcNow;
        return true;
    }

    public Task<bool> HasSuppliersInGroupAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        _db.Suppliers.AsNoTracking()
            .AnyAsync(s => !s.IsDeleted && s.SupplierGroupId == groupId, cancellationToken);

    private static SupplierDto Map(Supplier s) =>
        new(
            s.Id,
            s.Code,
            s.Name,
            s.Phone,
            s.Email,
            s.Address,
            s.Region,
            s.Ward,
            s.SupplierGroupId,
            s.SupplierGroup?.Name,
            s.Notes,
            s.CompanyName,
            s.TaxCode,
            s.InitialDebt,
            s.TotalPurchase,
            s.TotalReturn,
            s.CurrentDebt,
            s.IsActive,
            s.CreatedDate,
            s.CreatedBy,
            s.UpdatedDate,
            s.UpdatedBy);
}
