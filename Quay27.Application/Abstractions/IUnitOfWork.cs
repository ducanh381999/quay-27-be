namespace Quay27.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="action"/> inside a DB transaction, wrapped in EF execution strategy
    /// (required when retry-on-failure is enabled, e.g. MySQL). Do not call Begin/Commit/Rollback on this UoW inside <paramref name="action"/>.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExecuteInTransactionAsync"/>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);
}
