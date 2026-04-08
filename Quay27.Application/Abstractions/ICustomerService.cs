using Quay27.Application.Customers;

namespace Quay27.Application.Abstractions;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> ListBySheetDateAsync(DateOnly? sheetDate, int? queueId, bool pendingExport27 = false,
        string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<ImportCustomersExcelResult> ImportExcelAsync(ImportCustomersExcelRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetQueueAsync(Guid customerId, int queueId, SetCustomerQueueRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportGridAsync(ExportGridRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerAuditLogEntryDto>> GetAuditLogsForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
