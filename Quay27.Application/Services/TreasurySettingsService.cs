using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Abstractions;
using Quay27.Application.Repositories;
using Quay27.Application.Settings;
using Quay27.Domain.Constants;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class TreasurySettingsService : ITreasurySettingsService
{
    private readonly ITreasurySettingsRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateTreasuryAccountRequest> _createValidator;
    private readonly IValidator<PatchTreasuryAccountRequest> _patchValidator;

    public TreasurySettingsService(
        ITreasurySettingsRepository repo,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateTreasuryAccountRequest> createValidator,
        IValidator<PatchTreasuryAccountRequest> patchValidator)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _patchValidator = patchValidator;
    }

    public async Task<IReadOnlyList<BankCatalogItemDto>> ListBankCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _repo.ListBankCatalogAsync(cancellationToken);
        return items.Select(x => new BankCatalogItemDto(x.Id, x.Code, x.FullName, x.GlobalName, x.OrderNum)).ToList();
    }

    public async Task<IReadOnlyList<EWalletCatalogItemDto>> ListEWalletCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _repo.ListEWalletCatalogAsync(cancellationToken);
        return items.Select(x => new EWalletCatalogItemDto(x.Id, x.Code, x.FullName, x.GlobalName, x.OrderNum, x.Type))
            .ToList();
    }

    public async Task<IReadOnlyList<TreasuryAccountListItemDto>> ListAccountsAsync(string kind,
        CancellationToken cancellationToken = default)
    {
        var accountKind = NormalizeKind(kind);
        var items = await _repo.ListAccountsByKindAsync(accountKind, cancellationToken);
        return items.Select(MapListItem).ToList();
    }

    public async Task<TreasuryAccountDetailDto?> GetAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetAccountByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return null;
        return MapDetail(entity);
    }

    public async Task<TreasuryAccountDetailDto> CreateAsync(CreateTreasuryAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var vr = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid)
            throw new ValidationException(vr.Errors);

        var accountKind = NormalizeKind(request.Kind);
        var providerCode = request.ProviderCode.Trim();
        string displayName;
        if (accountKind == TreasuryConstants.AccountKindBank)
        {
            var bank = await _repo.GetBankByCodeAsync(providerCode, cancellationToken);
            if (bank is null)
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(request.ProviderCode), "Ngân hàng không hợp lệ.") });
            displayName = bank.FullName;
        }
        else
        {
            var wallet = await _repo.GetEWalletByCodeAsync(providerCode, cancellationToken);
            if (wallet is null)
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(request.ProviderCode), "Ví điện tử không hợp lệ.") });
            displayName = wallet.FullName;
        }

        var scopeKind = NormalizeScope(request.ScopeKind);
        var createdBy = string.IsNullOrWhiteSpace(_currentUser.Username) ? "system" : _currentUser.Username.Trim();
        var entity = new ReceivingAccount
        {
            Id = Guid.NewGuid(),
            Name = request.AccountHolderName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            BankName = displayName,
            AccountKind = accountKind,
            ProviderCode = providerCode,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            ScopeKind = scopeKind,
            BranchId = scopeKind == TreasuryConstants.ScopeBranch ? request.BranchId : null,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedDate = null,
            UpdatedBy = null,
        };

        await _repo.AddAccountAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapDetail(entity);
    }

    public async Task<TreasuryAccountDetailDto> PatchAsync(Guid id, PatchTreasuryAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var vr = await _patchValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid)
            throw new ValidationException(vr.Errors);

        var entity = await _repo.GetAccountByIdTrackedAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            throw new ValidationException(new[] { new ValidationFailure("id", "Không tìm thấy tài khoản.") });

        if (request.AccountNumber is not null)
            entity.AccountNumber = request.AccountNumber.Trim();
        if (request.AccountHolderName is not null)
            entity.Name = request.AccountHolderName.Trim();
        if (request.Note is not null)
            entity.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (request.ScopeKind is not null)
        {
            entity.ScopeKind = NormalizeScope(request.ScopeKind);
            if (entity.ScopeKind == TreasuryConstants.ScopeSystemWide)
                entity.BranchId = null;
        }

        if (request.BranchId.HasValue && entity.ScopeKind == TreasuryConstants.ScopeBranch)
            entity.BranchId = request.BranchId;

        if (request.ProviderCode is not null)
        {
            var code = request.ProviderCode.Trim();
            entity.ProviderCode = code;
            if (entity.AccountKind == TreasuryConstants.AccountKindBank)
            {
                var bank = await _repo.GetBankByCodeAsync(code, cancellationToken);
                if (bank is null)
                    throw new ValidationException(new[]
                        { new ValidationFailure(nameof(request.ProviderCode), "Ngân hàng không hợp lệ.") });
                entity.BankName = bank.FullName;
            }
            else
            {
                var wallet = await _repo.GetEWalletByCodeAsync(code, cancellationToken);
                if (wallet is null)
                    throw new ValidationException(new[]
                        { new ValidationFailure(nameof(request.ProviderCode), "Ví điện tử không hợp lệ.") });
                entity.BankName = wallet.FullName;
            }
        }

        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy = string.IsNullOrWhiteSpace(_currentUser.Username) ? "system" : _currentUser.Username.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetAccountByIdTrackedAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            throw new ValidationException(new[] { new ValidationFailure("id", "Không tìm thấy tài khoản.") });

        entity.IsActive = false;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy = string.IsNullOrWhiteSpace(_currentUser.Username) ? "system" : _currentUser.Username.Trim();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeKind(string kind)
    {
        var k = kind.Trim().ToLowerInvariant();
        return k switch
        {
            "bank" => TreasuryConstants.AccountKindBank,
            "ewallet" => TreasuryConstants.AccountKindEWallet,
            _ => throw new ValidationException(new[]
            {
                new ValidationFailure("kind", "kind phải là bank hoặc ewallet."),
            }),
        };
    }

    private static string NormalizeScope(string scopeKind)
    {
        var s = scopeKind.Trim();
        if (string.Equals(s, TreasuryConstants.ScopeBranch, StringComparison.OrdinalIgnoreCase))
            return TreasuryConstants.ScopeBranch;
        return TreasuryConstants.ScopeSystemWide;
    }

    private static TreasuryAccountListItemDto MapListItem(ReceivingAccount x) => new(
        x.Id,
        x.AccountNumber,
        x.ProviderCode ?? string.Empty,
        x.BankName,
        x.Name,
        x.ScopeKind,
        ScopeLabel(x.ScopeKind),
        x.Note,
        x.IsActive);

    private static TreasuryAccountDetailDto MapDetail(ReceivingAccount x) => new(
        x.Id,
        x.AccountKind == TreasuryConstants.AccountKindEWallet ? "ewallet" : "bank",
        x.AccountNumber,
        x.ProviderCode ?? string.Empty,
        x.BankName,
        x.Name,
        x.ScopeKind,
        x.BranchId,
        x.Note,
        x.IsActive);

    private static string ScopeLabel(string scopeKind) =>
        scopeKind == TreasuryConstants.ScopeBranch ? "Chi nhánh" : "Toàn hệ thống";
}
