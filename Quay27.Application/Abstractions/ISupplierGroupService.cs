using Quay27.Application.Suppliers;

namespace Quay27.Application.Abstractions;

public interface ISupplierGroupService
{
    Task<IReadOnlyList<SupplierGroupDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<SupplierGroupDto> CreateAsync(CreateSupplierGroupRequest request, CancellationToken cancellationToken = default);
    Task<SupplierGroupDto> UpdateAsync(Guid id, UpdateSupplierGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
