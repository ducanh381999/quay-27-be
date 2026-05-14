namespace Quay27.Application.Settings;

public sealed record BankCatalogItemDto(
    int Id,
    string Code,
    string FullName,
    string? GlobalName,
    int OrderNum);

public sealed record EWalletCatalogItemDto(
    int Id,
    string Code,
    string FullName,
    string? GlobalName,
    int OrderNum,
    int? Type);

public sealed record TreasuryAccountListItemDto(
    Guid Id,
    string AccountNumber,
    string ProviderCode,
    string BankDisplayName,
    string AccountHolderName,
    string ScopeKind,
    string ScopeLabel,
    string? Note,
    bool IsActive);

public sealed record TreasuryAccountDetailDto(
    Guid Id,
    string Kind,
    string AccountNumber,
    string ProviderCode,
    string BankDisplayName,
    string AccountHolderName,
    string ScopeKind,
    Guid? BranchId,
    string? Note,
    bool IsActive);

public sealed record CreateTreasuryAccountRequest(
    string Kind,
    string ProviderCode,
    string AccountNumber,
    string AccountHolderName,
    string? Note,
    string ScopeKind,
    Guid? BranchId);

public sealed record PatchTreasuryAccountRequest(
    string? ProviderCode,
    string? AccountNumber,
    string? AccountHolderName,
    string? Note,
    string? ScopeKind,
    Guid? BranchId);
