using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class CustomerDeliveryAddressRepository : ICustomerDeliveryAddressRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerDeliveryAddressRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerDeliveryAddress>> ListByCustomerAsync(Guid customerProfileId,
        CancellationToken cancellationToken = default) =>
        await _db.CustomerDeliveryAddresses.AsNoTracking()
            .Where(x => x.CustomerProfileId == customerProfileId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<CustomerDeliveryAddress?> GetTrackedByIdAsync(Guid customerProfileId, Guid addressId,
        CancellationToken cancellationToken = default) =>
        _db.CustomerDeliveryAddresses
            .FirstOrDefaultAsync(x => x.Id == addressId && x.CustomerProfileId == customerProfileId && !x.IsDeleted,
                cancellationToken);

    public Task AddAsync(CustomerDeliveryAddress entity, CancellationToken cancellationToken = default) =>
        _db.CustomerDeliveryAddresses.AddAsync(entity, cancellationToken).AsTask();
}
