namespace Quay27.Application.Repositories;

public interface IColumnPermissionRepository
{
    Task<bool> CanEditColumnAsync(Guid userId, string tableName, string columnName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ColumnPermissionRow>> ListByUserAndTableAsync(Guid userId, string tableName,
        CancellationToken cancellationToken = default);

    Task ReplaceForUserAndTableAsync(Guid userId, string tableName, IReadOnlyList<ColumnPermissionRow> rows,
        CancellationToken cancellationToken = default);
}

/// <summary>Minimal row for list/replace (no entity id exposed to API for list; ids regenerated on replace).</summary>
public record ColumnPermissionRow(string ColumnName, bool CanView, bool CanEdit);
