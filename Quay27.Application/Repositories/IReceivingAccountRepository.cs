using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IReceivingAccountRepository
{
    Task<IReadOnlyList<ReceivingAccount>> ListActiveAsync(CancellationToken cancellationToken = default);
    Task<ReceivingAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
