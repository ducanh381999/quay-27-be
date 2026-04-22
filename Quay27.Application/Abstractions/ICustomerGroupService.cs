using Quay27.Application.CustomerGroups;

namespace Quay27.Application.Abstractions;

public interface ICustomerGroupService
{
    Task<IReadOnlyList<CustomerGroupDto>> ListAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<CustomerGroupDto> CreateAsync(CreateCustomerGroupRequest request, CancellationToken cancellationToken = default);
    Task<CustomerGroupDto> UpdateAsync(Guid id, UpdateCustomerGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
