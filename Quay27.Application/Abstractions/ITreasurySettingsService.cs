using Quay27.Application.Settings;

namespace Quay27.Application.Abstractions;

public interface ITreasurySettingsService
{
    Task<IReadOnlyList<BankCatalogItemDto>> ListBankCatalogAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EWalletCatalogItemDto>> ListEWalletCatalogAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TreasuryAccountListItemDto>> ListAccountsAsync(string kind,
        CancellationToken cancellationToken = default);

    Task<TreasuryAccountDetailDto?> GetAccountAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TreasuryAccountDetailDto> CreateAsync(CreateTreasuryAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<TreasuryAccountDetailDto> PatchAsync(Guid id, PatchTreasuryAccountRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
