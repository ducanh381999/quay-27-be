using Microsoft.EntityFrameworkCore;
using Quay27.Application.CustomerProfiles;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public partial class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerProfileRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<CustomerProfileListPageRow> Items, int TotalCount)> ListPagedAsync(
        CustomerProfileListQuery query,
        CancellationToken cancellationToken = default)
    {
        var sf = query.SalesActivityFromUtc;
        var st = query.SalesActivityToUtc;
        var useSalesWindow = sf.HasValue || st.HasValue;

        var invBase = _db.SalesInvoices.AsNoTracking()
            .Where(i => i.CustomerProfileId != null && i.Status != "cancelled");

        var profiles = ApplyListFilters(_db.CustomerProfiles.AsNoTracking(), query);

        // Paid-in-window: when no date filter, winQ == invBase so PaidInWindow matches TotalPaidAll (EF-translatable).
        var winQ = invBase;
        if (useSalesWindow)
        {
            if (sf.HasValue)
            {
                var fromUtc = sf.Value;
                winQ = winQ.Where(i => i.CreatedAtUtc >= fromUtc);
            }

            if (st.HasValue)
            {
                var toUtc = st.Value;
                winQ = winQ.Where(i => i.CreatedAtUtc <= toUtc);
            }
        }

        var retBase = _db.SalesReturns.AsNoTracking()
            .Where(r => r.CustomerProfileId != null && r.Status != "cancelled");

        // Correlated scalar subqueries per profile — avoids LEFT JOIN + GroupBy shapes where joined `Id`
        // is NULL for customers with no invoices/returns, which EF materializes as non-nullable Guid and throws.
        var joined = profiles.Select(p => new
        {
            Profile = p,
            TotalPaidAll = invBase.Where(i => i.CustomerProfileId == p.Id)
                .Sum(i => (decimal?)i.PaidAmount) ?? 0m,
            InvoiceDebt = invBase.Where(i => i.CustomerProfileId == p.Id)
                .Sum(i => (decimal?)(i.SubtotalAmount - i.DiscountAmount - i.PaidAmount)) ?? 0m,
            ReturnTotal = retBase.Where(r => r.CustomerProfileId == p.Id)
                .Sum(r => (decimal?)r.Amount) ?? 0m,
            DisplayDebt = p.ManualCurrentDebt ?? (invBase.Where(i => i.CustomerProfileId == p.Id)
                .Sum(i => (decimal?)(i.SubtotalAmount - i.DiscountAmount - i.PaidAmount)) ?? 0m),
            LastInv = invBase.Where(i => i.CustomerProfileId == p.Id)
                .OrderByDescending(i => i.CreatedAtUtc)
                .Select(i => (DateTime?)i.CreatedAtUtc)
                .FirstOrDefault(),
            TotalPaidFilter = winQ.Where(i => i.CustomerProfileId == p.Id)
                .Sum(i => (decimal?)i.PaidAmount) ?? 0m,
        });

        var filtered = joined;

        if (query.LastInvoiceAtFrom.HasValue)
        {
            var from = query.LastInvoiceAtFrom.Value;
            filtered = filtered.Where(x => x.LastInv != null && x.LastInv >= from);
        }

        if (query.LastInvoiceAtTo.HasValue)
        {
            var to = query.LastInvoiceAtTo.Value;
            filtered = filtered.Where(x => x.LastInv != null && x.LastInv <= to);
        }

        if (query.TotalSalesMin.HasValue)
        {
            var min = query.TotalSalesMin.Value;
            filtered = filtered.Where(x => x.TotalPaidFilter >= min);
        }

        if (query.TotalSalesMax.HasValue)
        {
            var max = query.TotalSalesMax.Value;
            filtered = filtered.Where(x => x.TotalPaidFilter <= max);
        }

        if (query.CurrentDebtMin.HasValue)
        {
            var min = query.CurrentDebtMin.Value;
            filtered = filtered.Where(x => x.DisplayDebt >= min);
        }

        if (query.CurrentDebtMax.HasValue)
        {
            var max = query.CurrentDebtMax.Value;
            filtered = filtered.Where(x => x.DisplayDebt <= max);
        }

        var ordered = filtered
            .OrderByDescending(x => x.Profile.CreatedDate)
            .ThenBy(x => x.Profile.CustomerName);

        var totalCount = await ordered.CountAsync(cancellationToken);
        var take = Math.Clamp(query.Take <= 0 ? 50 : query.Take, 1, 200);
        var skip = Math.Max(0, query.Skip);

        var slice = await ordered
            .Skip(skip)
            .Take(take)
            .Select(x => new { x.Profile.Id, x.TotalPaidAll, x.InvoiceDebt, x.ReturnTotal })
            .ToListAsync(cancellationToken);

        if (slice.Count == 0)
            return (Array.Empty<CustomerProfileListPageRow>(), totalCount);

        var ids = slice.Select(s => s.Id).ToList();
        var profs = await _db.CustomerProfiles.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var dict = profs.ToDictionary(p => p.Id);

        var items = slice.Select(s => new CustomerProfileListPageRow
        {
            Profile = dict[s.Id],
            TotalPaid = s.TotalPaidAll,
            ReturnTotal = s.ReturnTotal,
            InvoiceDebt = s.InvoiceDebt,
        }).ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<CustomerProfile>> ListAsync(string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CustomerProfiles.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive);
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
            .AnyAsync(x => !x.IsDeleted && x.IsActive && x.Phone1 == p, cancellationToken);
    }

    public Task<bool> ExistsActiveByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var e = email.Trim();
        if (e.Length == 0)
            return Task.FromResult(false);
        return _db.CustomerProfiles.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.IsActive && x.Email == e, cancellationToken);
    }

    public Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.AddAsync(profile, cancellationToken).AsTask();

    public async Task<IReadOnlyList<string>> ListDistinctCreatorsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CustomerProfiles.AsNoTracking()
            .Where(x => !x.IsDeleted && x.CreatedBy != "")
            .Select(x => x.CreatedBy)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<CustomerProfile> ApplyListFilters(IQueryable<CustomerProfile> source,
        CustomerProfileListQuery query)
    {
        var q = source;

        var status = (query.Status ?? "active").Trim().ToLowerInvariant();
        if (status == "active")
            q = q.Where(x => !x.IsDeleted && x.IsActive);
        else if (status == "inactive")
            q = q.Where(x => x.IsDeleted || (!x.IsDeleted && !x.IsActive));

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

        if (!string.IsNullOrWhiteSpace(query.CreatedByContains))
        {
            var c = query.CreatedByContains.Trim().ToLowerInvariant();
            q = q.Where(x => x.CreatedBy.ToLower().Contains(c));
        }

        if (query.BirthdayFrom.HasValue)
            q = q.Where(x => x.Birthday != null && x.Birthday >= query.BirthdayFrom.Value);
        if (query.BirthdayTo.HasValue)
            q = q.Where(x => x.Birthday != null && x.Birthday <= query.BirthdayTo.Value);

        return q;
    }

    private static async Task<HashSet<Guid>> ToGuidHashSetAsync(IQueryable<Guid> query, CancellationToken ct) =>
        new HashSet<Guid>(await query.ToListAsync(ct));
}
