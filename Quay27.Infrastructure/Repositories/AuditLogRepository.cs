using Microsoft.EntityFrameworkCore;
using Quay27.Application.Customers;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _db;

    public AuditLogRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task AddRangeAsync(IEnumerable<AuditLog> entries, CancellationToken cancellationToken = default) =>
        _db.AuditLogs.AddRangeAsync(entries, cancellationToken);

    public async Task<IReadOnlyList<CustomerAuditLogEntryDto>> ListByRecordIdAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        return await _db.AuditLogs.AsNoTracking()
            .Where(x => x.RecordId == recordId)
            .OrderByDescending(x => x.ChangedDate)
            .Select(x => new CustomerAuditLogEntryDto
            {
                Id = x.Id,
                TableName = x.TableName,
                RecordId = x.RecordId,
                ColumnName = x.ColumnName,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                ActionType = x.ActionType,
                ChangedBy = x.ChangedBy,
                ChangedDate = x.ChangedDate
            })
            .ToListAsync(cancellationToken);
    }
}
