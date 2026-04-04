using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Quay27.Application.Abstractions;

namespace Quay27.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        // Note:
        // The project uses MySQL retry execution strategy, which doesn't support
        // user-initiated transactions via BeginTransactionAsync()/CommitTransactionAsync().
        //
        // We avoid explicit user transactions here. Each SaveChangesAsync call
        // will use EF's implicit transaction behavior when needed.
        await operation(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await action();
            return 0;
        }, cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(
            true,
            async (dbContext, _, ct) =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
                try
                {
                    var result = await action();
                    await transaction.CommitAsync(ct);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken: cancellationToken);
    }
}
