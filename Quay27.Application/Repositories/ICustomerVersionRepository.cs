using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerVersionRepository
{
    Task AddAsync(CustomerVersion version, CancellationToken cancellationToken = default);
}
