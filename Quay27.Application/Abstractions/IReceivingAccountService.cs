using Quay27.Application.Purchasing;

namespace Quay27.Application.Abstractions;

public interface IReceivingAccountService
{
    Task<IReadOnlyList<ReceivingAccountDto>> ListAsync(CancellationToken cancellationToken = default);
}
