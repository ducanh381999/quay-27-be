using Microsoft.EntityFrameworkCore;
using Quay27.Application.Customers;
using Quay27.Application.Repositories;
using Quay27.Domain.Constants;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Customer?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

    public async Task<CustomerDto?> GetProjectedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.Customers.AsNoTracking()
            .Include(c => c.CustomerQueues)
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<CustomerDto>> ListBySheetDateAsync(DateOnly? sheetDate, int? queueId, bool pendingExport27 = false,
        string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Customers.AsNoTracking()
            .Include(c => c.CustomerQueues)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var s = searchTerm.Trim();
            query = query.Where(c => c.NameAddress.Contains(s));
        }

        if (sheetDate is not null)
            query = query.Where(c => c.SheetDate == sheetDate.Value);

        if (queueId is not null)
        {
            query = query.Where(c => c.CustomerQueues.Any(cq => cq.QueueId == queueId));
            if (queueId == SchemaConstants.Quay27QueueId)
            {
                var limitDate = DateTime.UtcNow.AddDays(-30);
                query = query.Where(c => c.BillCreatedAt >= limitDate);
            }
        }

        if (pendingExport27)
        {
            if (queueId == SchemaConstants.Quay27QueueId)
            {
                // Ở màn hình Quầy 27: Lọc những đơn chưa xuất Excel
                query = query.Where(c => !c.Export27);
            }
            else
            {
                // Ở màn hình khác: Lọc những đơn chưa được gán vào Quầy 27
                var q27Id = SchemaConstants.Quay27QueueId;
                query = query.Where(c => !c.CustomerQueues.Any(cq => cq.QueueId == q27Id));
            }
        }

        var rows = await query
            .OrderBy(c => c.BillCreatedAt == DateTime.MinValue ? 1 : 0)
            .ThenByDescending(c => c.BillCreatedAt)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.InvoiceCode)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<CustomerDto>> ListTodayFullSheetWithCarryoverAsync(DateOnly today, bool pendingExport27 = false,
        string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var q27 = SchemaConstants.Quay27QueueId;
        var cancelled = SchemaConstants.CancelledInvoiceNotes;

        var query = _db.Customers.AsNoTracking()
            .Include(c => c.CustomerQueues)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var s = searchTerm.Trim();
            query = query.Where(c => c.NameAddress.Contains(s));
        }

        query = query.Where(c => c.SheetDate == today
                        || (c.SheetDate < today
                            && !c.FullSelfExport
                            && c.Notes != cancelled
                            && !c.CustomerQueues.Any(cq => cq.QueueId == q27)));

        if (pendingExport27)
        {
            // Ở Full Sheet, pendingExport27 có nghĩa là "Chưa cấp 27"
            query = query.Where(c => !c.CustomerQueues.Any(cq => cq.QueueId == q27));
        }

        // Notes: cancelled rows use exact literal match (same as FE).
        var rows = await query
            .OrderBy(c => c.BillCreatedAt == DateTime.MinValue ? 1 : 0)
            .ThenByDescending(c => c.BillCreatedAt)
            .ThenBy(c => c.SheetDate)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.InvoiceCode)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetActiveCustomerIdsWithSameNameAddressAsync(string nameAddress, CancellationToken cancellationToken = default)
    {
        var trimmed = nameAddress.Trim();
        var lower = trimmed.ToLowerInvariant();
        return await _db.Customers.AsNoTracking()
            .Where(c => !c.IsDeleted && c.NameAddress.Trim().ToLower() == lower)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        _db.Customers.AddAsync(customer, cancellationToken).AsTask();

    public async Task<bool> SoftDeleteAsync(Guid id, string updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        if (entity is null)
            return false;
        entity.IsDeleted = true;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.UtcNow;
        return true;
    }

    public async Task UpdateDuplicateStateAsync(IReadOnlyList<Guid> customerIds, bool isDuplicate, CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
            return;
        var list = await _db.Customers.Where(c => customerIds.Contains(c.Id)).ToListAsync(cancellationToken);
        foreach (var c in list)
            c.IsDuplicate = isDuplicate;
    }

    public async Task<int> AdvanceSheetDateForUnqueuedActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Customers
            .Where(c => !c.IsDeleted
                && !c.CustomerQueues.Any()
                && !c.Export27
                && !c.FullSelfExport
                && c.Notes != SchemaConstants.CancelledInvoiceNotes)
            .ToListAsync(cancellationToken);
        foreach (var c in list)
            c.SheetDate = c.SheetDate.AddDays(1);
        return list.Count;
    }

    private static CustomerDto Map(Customer c) =>
        new(
            c.Id,
            c.SortOrder,
            c.InvoiceCode,
            c.BillCreatedAt,
            c.NameAddress,
            c.CreateMachine,
            c.DraftStaff,
            c.Quantity,
            c.TotalAmount,
            c.InstallStaffCm,
            c.ManagerApproved,
            c.Kio27Received,
            c.Export27,
            c.FullSelfExport,
            c.Notes,
            c.GoodsSenderNote,
            c.AdditionalNotes,
            c.SheetDate,
            c.Status,
            c.IsDuplicate,
            c.CreatedDate,
            c.CreatedBy,
            c.UpdatedDate,
            c.UpdatedBy,
            c.CustomerQueues.Select(q => q.QueueId).Distinct().OrderBy(x => x).ToList());
}
