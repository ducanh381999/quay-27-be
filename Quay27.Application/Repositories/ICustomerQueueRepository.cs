using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerQueueRepository
{
    Task<CustomerQueue?> FindAsync(Guid customerId, int queueId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerQueue entity, CancellationToken cancellationToken = default);
    void Remove(CustomerQueue entity);
}
