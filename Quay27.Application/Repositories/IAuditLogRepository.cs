using Quay27.Application.Customers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IAuditLogRepository
{
    Task AddRangeAsync(IEnumerable<AuditLog> entries, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerAuditLogEntryDto>> ListByRecordIdAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);
}
