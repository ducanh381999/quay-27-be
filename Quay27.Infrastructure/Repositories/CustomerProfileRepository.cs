using Microsoft.EntityFrameworkCore;
using Quay27.Application.CustomerProfiles;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerProfileRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<CustomerProfile> Items, int TotalCount)> ListPagedAsync(
        CustomerProfileListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = ApplyListFilters(_db.CustomerProfiles.AsNoTracking(), query);
        q = q.OrderByDescending(x => x.CreatedDate).ThenBy(x => x.CustomerName);
        var totalCount = await q.CountAsync(cancellationToken);
        var take = Math.Clamp(query.Take <= 0 ? 50 : query.Take, 1, 200);
        var skip = Math.Max(0, query.Skip);
        var items = await q.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<CustomerProfile>> ListAsync(string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CustomerProfiles.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim();
            query = query.Where(x => x.CustomerName.Contains(t) || x.CustomerCode.Contains(t) || x.Phone1.Contains(t));
        }

        return await query
            .OrderByDescending(x => x.CreatedDate)
            .ThenBy(x => x.CustomerName)
            .ToListAsync(cancellationToken);
    }

    public Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<CustomerProfile?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<bool> CustomerCodeExistsAsync(string customerCode, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.AnyAsync(x => x.CustomerCode == customerCode, cancellationToken);

    public Task<bool> ExistsActiveByPhone1Async(string phone1, CancellationToken cancellationToken = default)
    {
        var p = phone1.Trim();
        if (p.Length == 0)
            return Task.FromResult(false);
        return _db.CustomerProfiles.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Phone1 == p, cancellationToken);
    }

    public Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.AddAsync(profile, cancellationToken).AsTask();

    private static IQueryable<CustomerProfile> ApplyListFilters(IQueryable<CustomerProfile> source,
        CustomerProfileListQuery query)
    {
        var q = source;

        var status = (query.Status ?? "active").Trim().ToLowerInvariant();
        if (status == "active")
            q = q.Where(x => !x.IsDeleted);
        else if (status == "inactive")
            q = q.Where(x => x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var t = query.Search.Trim();
            q = q.Where(x =>
                x.CustomerName.Contains(t) || x.CustomerCode.Contains(t) || x.Phone1.Contains(t));
        }

        if (query.CustomerGroups is { Count: > 0 })
        {
            var names = query.CustomerGroups
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .Distinct()
                .ToList();
            if (names.Count > 0)
                q = q.Where(x => names.Contains(x.CustomerGroup));
        }

        if (query.CreatedFrom.HasValue)
            q = q.Where(x => x.CreatedDate >= query.CreatedFrom.Value);
        if (query.CreatedTo.HasValue)
            q = q.Where(x => x.CreatedDate <= query.CreatedTo.Value);

        if (!string.IsNullOrWhiteSpace(query.BuyerType))
            q = q.Where(x => x.BuyerType == query.BuyerType.Trim());

        if (!string.IsNullOrWhiteSpace(query.Gender))
            q = q.Where(x => x.Gender == query.Gender.Trim());

        if (!string.IsNullOrWhiteSpace(query.DeliveryAreaText))
        {
            var d = query.DeliveryAreaText.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.Address.ToLower().Contains(d) ||
                x.Ward.ToLower().Contains(d) ||
                x.ProvinceCity.ToLower().Contains(d) ||
                x.InvoiceAddress.ToLower().Contains(d) ||
                x.InvoiceWard.ToLower().Contains(d) ||
                x.InvoiceProvinceCity.ToLower().Contains(d));
        }

        return q;
    }

    public async Task<IReadOnlyDictionary<Guid, CustomerProfileSalesAggregate>> GetSalesAggregatesByProfileIdsAsync(
        IReadOnlyList<Guid> profileIds,
        CancellationToken cancellationToken = default)
    {
        if (profileIds.Count == 0)
            return new Dictionary<Guid, CustomerProfileSalesAggregate>();

        var ids = profileIds.Distinct().ToArray();

        var invoiceAgg = await _db.SalesInvoices.AsNoTracking()
            .Where(i => i.CustomerProfileId != null && ids.Contains(i.CustomerProfileId.Value) &&
                        i.Status != "cancelled")
            .GroupBy(i => i.CustomerProfileId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                TotalPaid = g.Sum(x => x.PaidAmount),
                Debt = g.Sum(x => x.SubtotalAmount - x.DiscountAmount - x.PaidAmount),
            })
            .ToListAsync(cancellationToken);

        var returnAgg = await _db.SalesReturns.AsNoTracking()
            .Where(r => r.CustomerProfileId != null && ids.Contains(r.CustomerProfileId.Value) &&
                        r.Status != "cancelled")
            .GroupBy(r => r.CustomerProfileId!.Value)
            .Select(g => new { Id = g.Key, ReturnTotal = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var retMap = returnAgg.ToDictionary(x => x.Id, x => x.ReturnTotal);
        var dict = new Dictionary<Guid, CustomerProfileSalesAggregate>();
        foreach (var row in invoiceAgg)
        {
            var ret = retMap.GetValueOrDefault(row.Id, 0m);
            dict[row.Id] = new CustomerProfileSalesAggregate(row.TotalPaid, ret, row.Debt);
        }

        foreach (var id in ids)
        {
            if (dict.ContainsKey(id))
                continue;
            var retOnly = retMap.GetValueOrDefault(id, 0m);
            if (retOnly != 0m)
                dict[id] = new CustomerProfileSalesAggregate(0m, retOnly, 0m);
        }

        return dict;
    }
}
