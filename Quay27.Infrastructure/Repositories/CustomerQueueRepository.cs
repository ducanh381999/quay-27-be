using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class CustomerQueueRepository : ICustomerQueueRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerQueueRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<CustomerQueue?> FindAsync(Guid customerId, int queueId, CancellationToken cancellationToken = default) =>
        _db.CustomerQueues.FirstOrDefaultAsync(cq => cq.CustomerId == customerId && cq.QueueId == queueId, cancellationToken);

    public Task AddAsync(CustomerQueue entity, CancellationToken cancellationToken = default) =>
        _db.CustomerQueues.AddAsync(entity, cancellationToken).AsTask();

    public void Remove(CustomerQueue entity) => _db.CustomerQueues.Remove(entity);
}
