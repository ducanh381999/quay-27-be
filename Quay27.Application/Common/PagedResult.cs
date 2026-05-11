namespace Quay27.Application.Common;

/// <summary>
/// Standard list envelope for server-side pagination (skip/take).
/// Used by CRM customer profiles grid and intended for other dashboard lists.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
}
