using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class ColumnPermissionRepository : IColumnPermissionRepository
{
    private readonly ApplicationDbContext _db;

    public ColumnPermissionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<bool> CanEditColumnAsync(Guid userId, string tableName, string columnName, CancellationToken cancellationToken = default) =>
        _db.ColumnPermissions.AsNoTracking()
            .AnyAsync(p => p.UserId == userId && p.TableName == tableName && p.ColumnName == columnName && p.CanEdit,
                cancellationToken);

    public async Task<IReadOnlyList<ColumnPermissionRow>> ListByUserAndTableAsync(Guid userId, string tableName,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.ColumnPermissions.AsNoTracking()
            .Where(p => p.UserId == userId && p.TableName == tableName)
            .OrderBy(p => p.ColumnName)
            .Select(p => new ColumnPermissionRow(p.ColumnName, p.CanView, p.CanEdit))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task ReplaceForUserAndTableAsync(Guid userId, string tableName, IReadOnlyList<ColumnPermissionRow> rows,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.ColumnPermissions.Where(p => p.UserId == userId && p.TableName == tableName)
            .ToListAsync(cancellationToken);
        _db.ColumnPermissions.RemoveRange(existing);

        foreach (var row in rows)
        {
            _db.ColumnPermissions.Add(new ColumnPermission
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TableName = tableName,
                ColumnName = row.ColumnName,
                CanView = row.CanView,
                CanEdit = row.CanEdit
            });
        }
    }
}
