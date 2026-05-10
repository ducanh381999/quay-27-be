using Microsoft.EntityFrameworkCore;
using Quay27.Application.Cashbook;
using Quay27.Application.Common;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class CashbookRepository : ICashbookRepository
{
    private readonly ApplicationDbContext _db;

    public CashbookRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CashbookEntryListItemDto>> ListEntriesAsync(CashbookListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = ApplyFilters(_db.CashbookEntries.AsNoTracking(), query, inPeriod: true);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        return await q
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CashbookEntryListItemDto(
                x.Id,
                x.Code,
                x.EntryType,
                x.FundType,
                x.OccurredAtUtc,
                x.Amount,
                x.PaymentCategory != null ? x.PaymentCategory.Name : null,
                x.CounterpartyDisplayName,
                x.Status,
                x.AffectsBusinessResult,
                x.SourceKind))
            .ToListAsync(cancellationToken);
    }

    public async Task<CashbookSummaryDto> GetSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? fundType,
        IReadOnlyList<string>? entryTypes,
        int? paymentCategoryId,
        IReadOnlyList<string>? statuses,
        bool? affectsBusinessResult,
        Guid? createdByUserId,
        Guid? staffUserId,
        string? searchCode,
        string? counterpartySearch,
        string? counterpartyPhone,
        IReadOnlyList<string>? partnerDebtModes,
        CancellationToken cancellationToken = default)
    {
        var query = new CashbookListQuery(
            fromUtc,
            toUtc,
            fundType,
            entryTypes,
            paymentCategoryId,
            statuses,
            affectsBusinessResult,
            createdByUserId,
            staffUserId,
            searchCode,
            counterpartySearch,
            counterpartyPhone,
            partnerDebtModes,
            1,
            1);

        decimal opening = 0m;
        if (fromUtc.HasValue)
        {
            var qOpen = ApplyFilters(_db.CashbookEntries.AsNoTracking(), query, inPeriod: false);
            qOpen = qOpen.Where(x => x.OccurredAtUtc < fromUtc.Value);
            var receipts = await qOpen.Where(x => x.EntryType == "Receipt").SumAsync(x => x.Amount, cancellationToken);
            var payments = await qOpen.Where(x => x.EntryType == "Payment").SumAsync(x => x.Amount, cancellationToken);
            opening = MoneyMath.Round(receipts - payments);
        }

        var qPeriod = ApplyFilters(_db.CashbookEntries.AsNoTracking(), query, inPeriod: true);
        var periodReceipts =
            await qPeriod.Where(x => x.EntryType == "Receipt").SumAsync(x => x.Amount, cancellationToken);
        var periodPayments =
            await qPeriod.Where(x => x.EntryType == "Payment").SumAsync(x => x.Amount, cancellationToken);

        var totalReceipts = MoneyMath.Round(periodReceipts);
        var totalPayments = MoneyMath.Round(periodPayments);
        var closing = MoneyMath.Round(opening + totalReceipts - totalPayments);

        return new CashbookSummaryDto(opening, totalReceipts, totalPayments, closing);
    }

    private static IQueryable<CashbookEntry> ApplyFilters(IQueryable<CashbookEntry> source,
        CashbookListQuery query,
        bool inPeriod)
    {
        var q = source;

        if (query.Statuses is { Count: > 0 })
            q = q.Where(x => query.Statuses.Contains(x.Status));
        else
            q = q.Where(x => x.Status == "paid");

        if (!string.IsNullOrWhiteSpace(query.FundType) &&
            !string.Equals(query.FundType, "total", StringComparison.OrdinalIgnoreCase))
        {
            var f = query.FundType.Trim();
            q = q.Where(x => x.FundType == f);
        }

        if (query.EntryTypes is { Count: > 0 })
            q = q.Where(x => query.EntryTypes.Contains(x.EntryType));

        if (query.PaymentCategoryId.HasValue)
            q = q.Where(x => x.PaymentCategoryId == query.PaymentCategoryId.Value);

        if (query.AffectsBusinessResult.HasValue)
            q = q.Where(x => x.AffectsBusinessResult == query.AffectsBusinessResult.Value);

        if (query.CreatedByUserId.HasValue)
            q = q.Where(x => x.CreatedByUserId == query.CreatedByUserId.Value);

        if (query.StaffUserId.HasValue)
            q = q.Where(x => x.StaffUserId == query.StaffUserId.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchCode))
        {
            var s = query.SearchCode.Trim();
            q = q.Where(x => x.Code.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(query.CounterpartySearch))
        {
            var s = query.CounterpartySearch.Trim();
            q = q.Where(x =>
                (x.CounterpartyDisplayName != null && x.CounterpartyDisplayName.Contains(s)) ||
                (x.CashbookParty != null && x.CashbookParty.Name.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(query.CounterpartyPhone))
        {
            var p = query.CounterpartyPhone.Trim();
            q = q.Where(x => x.CashbookParty != null && x.CashbookParty.Phone != null && x.CashbookParty.Phone.Contains(p));
        }

        if (query.PartnerDebtModes is { Count: > 0 } &&
            query.PartnerDebtModes.Count < 4)
        {
            q = q.Where(x => query.PartnerDebtModes.Contains(x.PartnerDebtMode));
        }

        if (inPeriod)
        {
            if (query.FromUtc.HasValue)
                q = q.Where(x => x.OccurredAtUtc >= query.FromUtc.Value);
            if (query.ToUtc.HasValue)
                q = q.Where(x => x.OccurredAtUtc <= query.ToUtc.Value);
        }

        return q;
    }

    public async Task<string> GenerateNextCodeAsync(string entryType, CancellationToken cancellationToken = default)
    {
        var prefix = string.Equals(entryType, "Payment", StringComparison.OrdinalIgnoreCase) ? "PC" : "PT";
        var maxCode = await _db.CashbookEntries
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .OrderByDescending(x => x.Code)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length <= prefix.Length ||
            !int.TryParse(maxCode[prefix.Length..], out var number))
            return $"{prefix}{1:D6}";

        return $"{prefix}{(number + 1).ToString().PadLeft(6, '0')}";
    }

    public Task AddEntryAsync(CashbookEntry entity, CancellationToken cancellationToken = default) =>
        _db.CashbookEntries.AddAsync(entity, cancellationToken).AsTask();

    public Task AddPartyAsync(CashbookParty entity, CancellationToken cancellationToken = default) =>
        _db.CashbookParties.AddAsync(entity, cancellationToken).AsTask();

    public Task<CashbookParty?> GetPartyByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.CashbookParties.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsEntryForSourceAsync(string sourceKind, Guid sourceId,
        CancellationToken cancellationToken = default) =>
        _db.CashbookEntries.AsNoTracking()
            .AnyAsync(x => x.SourceKind == sourceKind && x.SourceId == sourceId, cancellationToken);

    public async Task RemoveEntriesBySourceAsync(string sourceKind, Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.CashbookEntries.Where(x => x.SourceKind == sourceKind && x.SourceId == sourceId)
            .ToListAsync(cancellationToken);
        _db.CashbookEntries.RemoveRange(rows);
    }
}
