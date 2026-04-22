using Quay27.Application.CustomerProfiles;

namespace Quay27.Application.Abstractions;

public interface ICustomerProfileService
{
    Task<IReadOnlyList<CustomerProfileDto>> ListAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto> CreateAsync(CreateCustomerProfileRequest request, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto> PatchAsync(Guid id, PatchCustomerProfileRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
