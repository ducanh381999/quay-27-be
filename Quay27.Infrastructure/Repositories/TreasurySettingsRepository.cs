using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class TreasurySettingsRepository : ITreasurySettingsRepository
{
    private readonly ApplicationDbContext _db;

    public TreasurySettingsRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BankCatalogItem>> ListBankCatalogAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.BankCatalogItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.OrderNum)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task<IReadOnlyList<EWalletCatalogItem>> ListEWalletCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _db.EWalletCatalogItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.OrderNum)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return list;
    }

    public Task<BankCatalogItem?> GetBankByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _db.BankCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

    public Task<EWalletCatalogItem?> GetEWalletByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _db.EWalletCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

    public async Task<IReadOnlyList<ReceivingAccount>> ListAccountsByKindAsync(string accountKind,
        CancellationToken cancellationToken = default)
    {
        var list = await _db.ReceivingAccounts
            .AsNoTracking()
            .Where(x => x.AccountKind == accountKind && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return list;
    }

    public Task<ReceivingAccount?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.ReceivingAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<ReceivingAccount?> GetAccountByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.ReceivingAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAccountAsync(ReceivingAccount account, CancellationToken cancellationToken = default)
    {
        await _db.ReceivingAccounts.AddAsync(account, cancellationToken);
    }
}
