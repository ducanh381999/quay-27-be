using Quay27.Application.Customers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetProjectedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerDto>> ListBySheetDateAsync(DateOnly? sheetDate, int? queueId, bool pendingExport27 = false,
        CancellationToken cancellationToken = default);

    /// <summary>Full sheet for &quot;today&quot; (VN): rows for <paramref name="today"/> plus older pending rows.</summary>
    Task<IReadOnlyList<CustomerDto>> ListTodayFullSheetWithCarryoverAsync(DateOnly today, bool pendingExport27 = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetActiveCustomerIdsWithSameNameAddressAsync(string nameAddress, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, string updatedBy, CancellationToken cancellationToken = default);
    Task UpdateDuplicateStateAsync(IReadOnlyList<Guid> customerIds, bool isDuplicate, CancellationToken cancellationToken = default);
    Task<int> AdvanceSheetDateForUnqueuedActiveCustomersAsync(CancellationToken cancellationToken = default);
}
