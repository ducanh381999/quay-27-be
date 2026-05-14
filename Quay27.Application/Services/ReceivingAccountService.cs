using Quay27.Application.Abstractions;
using Quay27.Application.Purchasing;
using Quay27.Application.Repositories;

namespace Quay27.Application.Services;

public class ReceivingAccountService : IReceivingAccountService
{
    private readonly IReceivingAccountRepository _accounts;

    public ReceivingAccountService(IReceivingAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<IReadOnlyList<ReceivingAccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _accounts.ListActiveAsync(cancellationToken);
        return items
            .Select(x => new ReceivingAccountDto(x.Id, x.Name, x.AccountNumber, x.BankName, x.IsActive,
                x.ProviderCode))
            .ToList();
    }
}
