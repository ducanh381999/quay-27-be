using Quay27.Application.Customers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetProjectedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerDto>> ListBySheetDateAsync(DateOnly sheetDate, int? queueId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetActiveCustomerIdsWithSameNameAddressAsync(string nameAddress, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, string updatedBy, CancellationToken cancellationToken = default);
    Task UpdateDuplicateStateAsync(IReadOnlyList<Guid> customerIds, bool isDuplicate, CancellationToken cancellationToken = default);
    Task<int> AdvanceSheetDateForUnqueuedActiveCustomersAsync(CancellationToken cancellationToken = default);
}
