using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerDeliveryAddressRepository
{
    Task<IReadOnlyList<CustomerDeliveryAddress>> ListByCustomerAsync(Guid customerProfileId,
        CancellationToken cancellationToken = default);

    Task<CustomerDeliveryAddress?> GetTrackedByIdAsync(Guid customerProfileId, Guid addressId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CustomerDeliveryAddress entity, CancellationToken cancellationToken = default);
}
