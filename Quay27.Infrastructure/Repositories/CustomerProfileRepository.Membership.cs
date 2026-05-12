using Microsoft.EntityFrameworkCore;
using Quay27.Application.CustomerGroups;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public partial class CustomerProfileRepository
{
    public async Task<HashSet<Guid>> FindActiveProfileIdsMatchingGroupConditionsAsync(
        IReadOnlyList<CustomerGroupConditionDto> conditions,
        bool combineAll,
        CancellationToken cancellationToken = default)
    {
        if (conditions.Count == 0)
            return new HashSet<Guid>();

        var sets = new List<HashSet<Guid>>();
        foreach (var c in conditions)
            sets.Add(await QueryIdsForSingleConditionAsync(c, cancellationToken));

        if (combineAll)
        {
            var r = sets[0];
            for (var i = 1; i < sets.Count; i++)
                r.IntersectWith(sets[i]);
            return r;
        }

        var u = new HashSet<Guid>();
        foreach (var s in sets)
            u.UnionWith(s);
        return u;
    }

    public async Task ClearCustomerGroupFromProfilesNotInMatchingSetAsync(
        string groupName,
        IReadOnlyCollection<Guid> matchingProfileIds,
        CancellationToken cancellationToken = default)
    {
        var match = matchingProfileIds as HashSet<Guid> ?? matchingProfileIds.ToHashSet();
        if (match.Count == 0)
        {
            await _db.CustomerProfiles
                .Where(p => !p.IsDeleted && p.CustomerGroup == groupName)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.CustomerGroup, _ => string.Empty),
                    cancellationToken);
            return;
        }

        await _db.CustomerProfiles
            .Where(p => !p.IsDeleted && p.CustomerGroup == groupName && !match.Contains(p.Id))
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.CustomerGroup, _ => string.Empty),
                cancellationToken);
    }

    public async Task AssignCustomerGroupToProfileIdsAsync(
        string groupName,
        IReadOnlyCollection<Guid> profileIds,
        CancellationToken cancellationToken = default)
    {
        var distinct = profileIds.Distinct().ToArray();
        foreach (var chunk in distinct.Chunk(500))
        {
            var arr = chunk.ToArray();
            await _db.CustomerProfiles
                .Where(p => !p.IsDeleted && arr.Contains(p.Id))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.CustomerGroup, _ => groupName),
                    cancellationToken);
        }
    }

    private async Task<HashSet<Guid>> QueryIdsForSingleConditionAsync(
        CustomerGroupConditionDto c,
        CancellationToken cancellationToken)
    {
        var field = (c.Field ?? string.Empty).Trim();
        var op = NormalizeOp(c.Op);
        var val = c.Value?.Trim() ?? string.Empty;

        return field switch
        {
            "TotalInvoiced" => await CompareInvoicedNetAsync(op, val, cancellationToken),
            "TotalRevenue" => await CompareTotalRevenueAsync(op, val, cancellationToken),
            "PurchaseNumber" => await ComparePurchaseNumberAsync(op, val, cancellationToken),
            "Debt" => await CompareDebtAsync(op, val, cancellationToken),
            "RewardPoint" => await CompareRewardPointsBalanceAsync(op, val, cancellationToken),
            "TotalPoint" => await CompareRewardPointsLifetimeAsync(op, val, cancellationToken),
            "PurchaseDate" => await CompareLastPurchaseDateAsync(op, val, cancellationToken),
            "BirthDay" => await CompareBirthMonthAsync(val, cancellationToken),
            "Age" => await CompareAgeAsync(op, val, cancellationToken),
            "Gender" => await CompareGenderAsync(val, cancellationToken),
            "Location" => await CompareLocationAsync(val, cancellationToken),
            "Type" => await CompareBuyerTypeAsync(val, cancellationToken),
            _ => new HashSet<Guid>(),
        };
    }

    private static string NormalizeOp(string? op)
    {
        var o = (op ?? "=").Trim();
        return o == "==" ? "=" : o;
    }

    private static bool TryParseDecimal(string s, out decimal d) =>
        decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out d);

    private static bool TryParseInt(string s, out int n) =>
        int.TryParse(s, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out n);

    private IQueryable<CustomerProfile> ActiveProfiles() =>
        _db.CustomerProfiles.AsNoTracking().Where(p => !p.IsDeleted);

    private async Task<HashSet<Guid>> CompareInvoicedNetAsync(string op, string val, CancellationToken ct)
    {
        if (!TryParseDecimal(val, out var threshold))
            return new HashSet<Guid>();

        var invBase = _db.SalesInvoices.AsNoTracking()
            .Where(i => i.CustomerProfileId != null && i.Status != "cancelled");
        var invNet = invBase
            .GroupBy(i => i.CustomerProfileId!.Value)
            .Select(g => new { Id = g.Key, Gross = g.Sum(x => x.SubtotalAmount - x.DiscountAmount) });

        var joined =
            from p in ActiveProfiles()
            join n in invNet on p.Id equals n.Id into ng
            from n in ng.DefaultIfEmpty()
            select new { p.Id, gross = n != null ? n.Gross : 0m };

        return op switch
        {
            ">" => await joined.Where(x => x.gross > threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "<" => await joined.Where(x => x.gross < threshold).Select(x => x.Id).ToHashSetAsync(ct),
            ">=" => await joined.Where(x => x.gross >= threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "<=" => await joined.Where(x => x.gross <= threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "=" => await joined.Where(x => x.gross == threshold).Select(x => x.Id).ToHashSetAsync(ct),
            _ => new HashSet<Guid>(),
        };
    }

    private async Task<HashSet<Guid>> CompareTotalRevenueAsync(string op, string val, CancellationToken ct)
    {
        if (!TryParseDecimal(val, out var threshold))
            return new HashSet<Guid>();

        var invBase = _db.SalesInvoices.AsNoTracking()
            .Where(i => i.CustomerProfileId != null && i.Status != "cancelled");
        var invNet = invBase
            .GroupBy(i => i.CustomerProfileId!.Value)
            .Select(g => new { Id = g.Key, Gross = g.Sum(x => x.SubtotalAmount - x.DiscountAmount) });
        var retAgg = _db.SalesReturns.AsNoTracking()
            .Where(r => r.CustomerProfileId != null && r.Status != "cancelled")
            .GroupBy(r => r.CustomerProfileId!.Value)
            .Select(g => new { Id = g.Key, ReturnTotal = g.Sum(x => x.Amount) });

        var joined =
            from p in ActiveProfiles()
            join n in invNet on p.Id equals n.Id into ng
            from n in ng.DefaultIfEmpty()
            join r in retAgg on p.Id equals r.Id into rg
            from r in rg.DefaultIfEmpty()
            select new
            {
                p.Id,
                revenue = (n != null ? n.Gross : 0m) - (r != null ? r.ReturnTotal : 0m),
            };

        return op switch
        {
            ">" => await joined.Where(x => x.revenue > threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "<" => await joined.Where(x => x.revenue < threshold).Select(x => x.Id).ToHashSetAsync(ct),
            ">=" => await joined.Where(x => x.revenue >= threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "<=" => await joined.Where(x => x.revenue <= threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "=" => await joined.Where(x => x.revenue == threshold).Select(x => x.Id).ToHashSetAsync(ct),
            _ => new HashSet<Guid>(),
        };
    }

    private async Task<HashSet<Guid>> ComparePurchaseNumberAsync(string op, string val, CancellationToken ct)
    {
        if (!TryParseInt(val, out var threshold))
            return new HashSet<Guid>();

        var counts = _db.SalesInvoices.AsNoTracking()
            .Where(i => i.CustomerProfileId != null && i.Status != "cancelled")
            .GroupBy(i => i.CustomerProfileId!.Value)
            .Select(g => new { Id = g.Key, Cnt = g.Count() });

        var joined =
            from p in ActiveProfiles()
            join c in counts on p.Id equals c.Id into cg
            from c in cg.DefaultIfEmpty()
            select new { p.Id, cnt = c != null ? c.Cnt : 0 };

        return op switch
        {
            ">" => await joined.Where(x => x.cnt > threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "<" => await joined.Where(x => x.cnt < threshold).Select(x => x.Id).ToHashSetAsync(ct),
            ">=" => await joined.Where(x => x.cnt >= threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "<=" => await joined.Where(x => x.cnt <= threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "=" => await joined.Where(x => x.cnt == threshold).Select(x => x.Id).ToHashSetAsync(ct),
            _ => new HashSet<Guid>(),
        };
    }

    private async Task<HashSet<Guid>> CompareDebtAsync(string op, string val, CancellationToken ct)
    {
        if (!TryParseDecimal(val, out var threshold))
            return new HashSet<Guid>();

        var invBase = _db.SalesInvoices.AsNoTracking()
            .Where(i => i.CustomerProfileId != null && i.Status != "cancelled");
        var invDebt = invBase
            .GroupBy(i => i.CustomerProfileId!.Value)
            .Select(g => new { Id = g.Key, DebtAll = g.Sum(x => x.SubtotalAmount - x.DiscountAmount - x.PaidAmount) });

        var joined =
            from p in ActiveProfiles()
            join d in invDebt on p.Id equals d.Id into dg
            from d in dg.DefaultIfEmpty()
            select new { p.Id, display = p.ManualCurrentDebt ?? (d != null ? d.DebtAll : 0m) };

        return op switch
        {
            ">" => await joined.Where(x => x.display > threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "<" => await joined.Where(x => x.display < threshold).Select(x => x.Id).ToHashSetAsync(ct),
            ">=" => await joined.Where(x => x.display >= threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "<=" => await joined.Where(x => x.display <= threshold).Select(x => x.Id).ToHashSetAsync(ct),
            "=" => await joined.Where(x => x.display == threshold).Select(x => x.Id).ToHashSetAsync(ct),
            _ => new HashSet<Guid>(),
        };
    }

    private async Task<HashSet<Guid>> CompareRewardPointsBalanceAsync(string op, string val, CancellationToken ct)
    {
        if (!TryParseDecimal(val, out var threshold))
            return new HashSet<Guid>();
        var q = ActiveProfiles();
        return op switch
        {
            ">" => await q.Where(p => p.RewardPointsBalance > threshold).Select(p => p.Id).ToHashSetAsync(ct),
            "<" => await q.Where(p => p.RewardPointsBalance < threshold).Select(p => p.Id).ToHashSetAsync(ct),
            ">=" => await q.Where(p => p.RewardPointsBalance >= threshold).Select(p => p.Id).ToHashSetAsync(ct),
            "<=" => await q.Where(p => p.RewardPointsBalance <= threshold).Select(p => p.Id).ToHashSetAsync(ct),
            "=" => await q.Where(p => p.RewardPointsBalance == threshold).Select(p => p.Id).ToHashSetAsync(ct),
            _ => new HashSet<Guid>(),
        };
    }

    private async Task<HashSet<Guid>> CompareRewardPointsLifetimeAsync(string op, string val, CancellationToken ct)
    {
        if (!TryParseDecimal(val, out var threshold))
            return new HashSet<Guid>();
        var q = ActiveProfiles();
        return op switch
        {
            ">" => await q.Where(p => p.RewardPointsLifetime > threshold).Select(p => p.Id).ToHashSetAsync(ct),
            "<" => await q.Where(p => p.RewardPointsLifetime < threshold).Select(p => p.Id).ToHashSetAsync(ct),
            ">=" => await q.Where(p => p.RewardPointsLifetime >= threshold).Select(p => p.Id).ToHashSetAsync(ct),
            "<=" => await q.Where(p => p.RewardPointsLifetime <= threshold).Select(p => p.Id).ToHashSetAsync(ct),
            "=" => await q.Where(p => p.RewardPointsLifetime == threshold).Select(p => p.Id).ToHashSetAsync(ct),
            _ => new HashSet<Guid>(),
        };
    }

    private async Task<HashSet<Guid>> CompareLastPurchaseDateAsync(string op, string val, CancellationToken ct)
    {
        if (!DateOnly.TryParse(val, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var day))
            return new HashSet<Guid>();

        var thresholdUtc = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var lastInv = _db.SalesInvoices.AsNoTracking()
            .Where(i => i.CustomerProfileId != null && i.Status != "cancelled")
            .GroupBy(i => i.CustomerProfileId!.Value)
            .Select(g => new { Id = g.Key, Last = g.Max(x => x.CreatedAtUtc) });

        var joined =
            from p in ActiveProfiles()
            join l in lastInv on p.Id equals l.Id into lg
            from l in lg.DefaultIfEmpty()
            select new { p.Id, Last = l != null ? l.Last : (DateTime?)null };

        var thr = thresholdUtc;
        return op switch
        {
            ">" => await joined.Where(x => x.Last != null && x.Last > thr).Select(x => x.Id).ToHashSetAsync(ct),
            "<" => await joined.Where(x => x.Last != null && x.Last < thr).Select(x => x.Id).ToHashSetAsync(ct),
            ">=" => await joined.Where(x => x.Last != null && x.Last >= thr).Select(x => x.Id).ToHashSetAsync(ct),
            "<=" => await joined.Where(x => x.Last != null && x.Last <= thr).Select(x => x.Id).ToHashSetAsync(ct),
            "=" => await joined.Where(x => x.Last != null && x.Last >= thr && x.Last < thr.AddDays(1))
                .Select(x => x.Id).ToHashSetAsync(ct),
            _ => new HashSet<Guid>(),
        };
    }

    private async Task<HashSet<Guid>> CompareBirthMonthAsync(string val, CancellationToken ct)
    {
        if (!TryParseInt(val, out var month) || month is < 1 or > 12)
            return new HashSet<Guid>();

        return await ActiveProfiles()
            .Where(p => p.Birthday != null && p.Birthday.Value.Month == month)
            .Select(p => p.Id)
            .ToHashSetAsync(ct);
    }

    private async Task<HashSet<Guid>> CompareAgeAsync(string op, string val, CancellationToken ct)
    {
        if (!TryParseInt(val, out var ageYears))
            return new HashSet<Guid>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return op switch
        {
            ">=" => await ActiveProfiles()
                .Where(p => p.Birthday != null && p.Birthday <= today.AddYears(-ageYears))
                .Select(p => p.Id).ToHashSetAsync(ct),
            ">" => await ActiveProfiles()
                .Where(p => p.Birthday != null && p.Birthday < today.AddYears(-ageYears))
                .Select(p => p.Id).ToHashSetAsync(ct),
            "<=" => await ActiveProfiles()
                .Where(p => p.Birthday != null && p.Birthday >= today.AddYears(-ageYears))
                .Select(p => p.Id).ToHashSetAsync(ct),
            "<" => await ActiveProfiles()
                .Where(p => p.Birthday != null && p.Birthday > today.AddYears(-ageYears))
                .Select(p => p.Id).ToHashSetAsync(ct),
            "=" => await ActiveProfiles()
                .Where(p => p.Birthday != null && p.Birthday <= today.AddYears(-ageYears) &&
                            p.Birthday > today.AddYears(-ageYears - 1))
                .Select(p => p.Id).ToHashSetAsync(ct),
            _ => new HashSet<Guid>(),
        };
    }

    private async Task<HashSet<Guid>> CompareGenderAsync(string val, CancellationToken ct)
    {
        var v = val.Trim();
        if (v.Length == 0)
            return new HashSet<Guid>();
        return await ActiveProfiles().Where(p => p.Gender == v).Select(p => p.Id).ToHashSetAsync(ct);
    }

    private async Task<HashSet<Guid>> CompareBuyerTypeAsync(string val, CancellationToken ct)
    {
        var v = val.Trim();
        if (v is not ("individual" or "company"))
            return new HashSet<Guid>();
        return await ActiveProfiles().Where(p => p.BuyerType == v).Select(p => p.Id).ToHashSetAsync(ct);
    }

    private async Task<HashSet<Guid>> CompareLocationAsync(string val, CancellationToken ct)
    {
        var d = val.Trim().ToLowerInvariant();
        if (d.Length == 0)
            return new HashSet<Guid>();

        return await ActiveProfiles()
            .Where(p =>
                p.Address.ToLower().Contains(d) ||
                p.Ward.ToLower().Contains(d) ||
                p.ProvinceCity.ToLower().Contains(d) ||
                p.InvoiceAddress.ToLower().Contains(d) ||
                p.InvoiceWard.ToLower().Contains(d) ||
                p.InvoiceProvinceCity.ToLower().Contains(d))
            .Select(p => p.Id)
            .ToHashSetAsync(ct);
    }
}
