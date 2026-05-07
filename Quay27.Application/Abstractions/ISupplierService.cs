using Quay27.Application.Suppliers;

namespace Quay27.Application.Abstractions;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> ListAsync(ListSuppliersQuery query, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ImportSuppliersExcelResult> ImportExcelAsync(ImportSuppliersExcelRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportExcelAsync(ListSuppliersQuery query, CancellationToken cancellationToken = default);
}
