using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ITreasurySettingsRepository
{
    Task<IReadOnlyList<BankCatalogItem>> ListBankCatalogAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EWalletCatalogItem>> ListEWalletCatalogAsync(CancellationToken cancellationToken = default);

    Task<BankCatalogItem?> GetBankByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<EWalletCatalogItem?> GetEWalletByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReceivingAccount>> ListAccountsByKindAsync(string accountKind,
        CancellationToken cancellationToken = default);

    Task<ReceivingAccount?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReceivingAccount?> GetAccountByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAccountAsync(ReceivingAccount account, CancellationToken cancellationToken = default);
}
