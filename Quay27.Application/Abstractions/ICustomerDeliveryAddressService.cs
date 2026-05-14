using Quay27.Application.CustomerProfiles;

namespace Quay27.Application.Abstractions;

public interface ICustomerDeliveryAddressService
{
    Task<IReadOnlyList<CustomerDeliveryAddressDto>> ListAsync(Guid customerProfileId,
        CancellationToken cancellationToken = default);

    Task<CustomerDeliveryAddressDto> CreateAsync(Guid customerProfileId, CreateCustomerDeliveryAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerDeliveryAddressDto> PatchAsync(Guid customerProfileId, Guid addressId,
        PatchCustomerDeliveryAddressRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid customerProfileId, Guid addressId, CancellationToken cancellationToken = default);
}
