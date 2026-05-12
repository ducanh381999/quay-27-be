using Microsoft.EntityFrameworkCore;
using Quay27.Application.Abstractions;
using Quay27.Application.Dashboard;
using Quay27.Domain.Constants;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TodaySummaryDto> GetTodaySummaryAsync(
        DateTime? utcNow,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var range = DashboardDateRange.Resolve(DashboardPreset.Today, now);
        var (startUtc, endUtc) = DashboardDateRange.ToUtcHalfOpenInterval(range, now);

        var rows = await LoadSaleRowsAsync(startUtc, endUtc, cancellationToken);

        decimal revenue = 0;
        var invoiceCount = 0;
        decimal returnAmount = 0;
        var returnCount = 0;

        foreach (var r in rows)
        {
            if (IsCancelled(r.Notes))
                continue;

            var amt = VietnameseMoneyParser.ParseDecimalOrZero(r.TotalAmount);

            if (SalesDashboardConstants.IsSalesReturn(r.Status, r.Notes))
            {
                returnAmount += amt;
                returnCount++;
                continue;
            }

            revenue += amt;
            invoiceCount++;
        }

        return new TodaySummaryDto(revenue, invoiceCount, returnAmount, returnCount);
    }

    public async Task<NetRevenueSeriesDto> GetNetRevenueSeriesAsync(
        DashboardPreset preset,
        DashboardBucket bucket,
        DateTime? utcNow,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var range = DashboardDateRange.Resolve(preset, now);
        var (startUtc, endUtc) = DashboardDateRange.ToUtcHalfOpenInterval(range, now);

        var rows = await LoadSaleRowsAsync(startUtc, endUtc, cancellationToken);
        var normal = rows
            .Where(r => !IsCancelled(r.Notes) && !SalesDashboardConstants.IsSalesReturn(r.Status, r.Notes))
            .ToList();

        var points = BuildSeries(bucket, range, normal);
        var total = normal.Sum(r => VietnameseMoneyParser.ParseDecimalOrZero(r.TotalAmount));
        return new NetRevenueSeriesDto(total, points);
    }

    public async Task<IReadOnlyList<TopRankRowDto>> GetTopCustomersAsync(
        DashboardPreset preset,
        DateTime? utcNow,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var range = DashboardDateRange.Resolve(preset, now);
        var (startUtc, endUtc) = DashboardDateRange.ToUtcHalfOpenInterval(range, now);

        var rows = await LoadSaleRowsAsync(startUtc, endUtc, cancellationToken);
        return rows
            .Where(r => !IsCancelled(r.Notes) && !SalesDashboardConstants.IsSalesReturn(r.Status, r.Notes))
            .GroupBy(r => r.NameAddress.Trim())
            .Select(g => new TopRankRowDto(
                string.IsNullOrEmpty(g.Key) ? "(Không tên)" : g.Key,
                g.Sum(x => VietnameseMoneyParser.ParseDecimalOrZero(x.TotalAmount))))
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();
    }

    public async Task<IReadOnlyList<TopRankRowDto>> GetTopProductsAsync(
        DashboardPreset preset,
        DashboardProductMetric metric,
        DateTime? utcNow,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var range = DashboardDateRange.Resolve(preset, now);
        var (startUtc, endUtc) = DashboardDateRange.ToUtcHalfOpenInterval(range, now);

        // EF cannot translate DashboardService.IsCancelled / SalesDashboardConstants.IsSalesReturn — use translatable predicates.
        var statusMarkersLower = SalesDashboardConstants.SalesReturnStatusContainsMarkers
            .Where(m => !string.IsNullOrEmpty(m))
            .Select(m => m.ToLowerInvariant())
            .ToList();
        var noteMarkersLower = SalesDashboardConstants.SalesReturnNotesContainsMarkers
            .Where(m => !string.IsNullOrEmpty(m))
            .Select(m => m.ToLowerInvariant())
            .ToList();

        // Avoid `localList.Any(m => column.ToLower().Contains(m))` when marker lists are empty: some providers
        // (e.g. EF InMemory in tests) do not translate that shape; with no markers it matches IsSalesReturn anyway.
        var lineRows = statusMarkersLower.Count == 0 && noteMarkersLower.Count == 0
            ? await (
                from line in _db.CustomerInvoiceLines.AsNoTracking()
                join c in _db.Customers.AsNoTracking() on line.CustomerId equals c.Id
                where !c.IsDeleted
                      && c.BillCreatedAt >= startUtc
                      && c.BillCreatedAt < endUtc
                      && c.BillCreatedAt > DateTime.MinValue
                      && c.Notes.Trim() != SchemaConstants.CancelledInvoiceNotes
                select new
                {
                    line.ProductId,
                    line.ProductNameSnapshot,
                    line.Amount,
                    line.Quantity,
                }).ToListAsync(cancellationToken)
            : await (
                from line in _db.CustomerInvoiceLines.AsNoTracking()
                join c in _db.Customers.AsNoTracking() on line.CustomerId equals c.Id
                where !c.IsDeleted
                      && c.BillCreatedAt >= startUtc
                      && c.BillCreatedAt < endUtc
                      && c.BillCreatedAt > DateTime.MinValue
                      && c.Notes.Trim() != SchemaConstants.CancelledInvoiceNotes
                      && (statusMarkersLower.Count == 0 ||
                          !statusMarkersLower.Any(m => c.Status.ToLower().Contains(m)))
                      && (noteMarkersLower.Count == 0 ||
                          !noteMarkersLower.Any(m => c.Notes.ToLower().Contains(m)))
                select new
                {
                    line.ProductId,
                    line.ProductNameSnapshot,
                    line.Amount,
                    line.Quantity,
                }).ToListAsync(cancellationToken);

        var list = lineRows
            .GroupBy(x => new { x.ProductId, x.ProductNameSnapshot })
            .Select(g => new
            {
                g.Key.ProductNameSnapshot,
                Amount = g.Sum(x => x.Amount),
                Qty = g.Sum(x => x.Quantity),
            })
            .ToList();

        IEnumerable<TopRankRowDto> ranked = metric switch
        {
            DashboardProductMetric.Quantity => list
                .OrderByDescending(x => x.Qty)
                .Take(10)
                .Select(x => new TopRankRowDto(
                    string.IsNullOrWhiteSpace(x.ProductNameSnapshot) ? "(Hàng)" : x.ProductNameSnapshot,
                    x.Qty)),
            _ => list
                .OrderByDescending(x => x.Amount)
                .Take(10)
                .Select(x => new TopRankRowDto(
                    string.IsNullOrWhiteSpace(x.ProductNameSnapshot) ? "(Hàng)" : x.ProductNameSnapshot,
                    x.Amount)),
        };

        return ranked.ToList();
    }

    private sealed record SaleRow(
        DateTime BillCreatedAt,
        string TotalAmount,
        string NameAddress,
        string Notes,
        string Status);

    private async Task<List<SaleRow>> LoadSaleRowsAsync(
        DateTime startUtc,
        DateTime endUtcExclusive,
        CancellationToken cancellationToken) =>
        await _db.Customers.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Where(c => c.BillCreatedAt >= startUtc && c.BillCreatedAt < endUtcExclusive)
            .Where(c => c.BillCreatedAt > DateTime.MinValue)
            .Select(c => new SaleRow(c.BillCreatedAt, c.TotalAmount, c.NameAddress, c.Notes, c.Status))
            .ToListAsync(cancellationToken);

    private static bool IsCancelled(string notes) =>
        string.Equals(notes.Trim(), SchemaConstants.CancelledInvoiceNotes, StringComparison.Ordinal);

    private static IReadOnlyList<ChartPointDto> BuildSeries(
        DashboardBucket bucket,
        DateRangeYmd range,
        IReadOnlyList<SaleRow> normalSales)
    {
        return bucket switch
        {
            DashboardBucket.Hour => BuildHourSeries(normalSales),
            DashboardBucket.Weekday => BuildWeekdaySeries(normalSales),
            _ => BuildDaySeries(range, normalSales),
        };
    }

    private static IReadOnlyList<ChartPointDto> BuildHourSeries(IReadOnlyList<SaleRow> normalSales)
    {
        var sums = new decimal[24];
        foreach (var r in normalSales)
        {
            var vn = DashboardDateRange.ToVietnamWallFromBillUtc(r.BillCreatedAt);
            sums[vn.Hour] += VietnameseMoneyParser.ParseDecimalOrZero(r.TotalAmount);
        }

        return Enumerable.Range(0, 24)
            .Select(h => new ChartPointDto($"{h:D2}h", sums[h]))
            .ToList();
    }

    private static IReadOnlyList<ChartPointDto> BuildDaySeries(DateRangeYmd range, IReadOnlyList<SaleRow> normalSales)
    {
        var dict = new Dictionary<DateOnly, decimal>();
        for (var d = range.From; d <= range.To; d = d.AddDays(1))
            dict[d] = 0;

        foreach (var r in normalSales)
        {
            var vn = DashboardDateRange.ToVietnamWallFromBillUtc(r.BillCreatedAt);
            var day = DateOnly.FromDateTime(vn);
            if (!dict.ContainsKey(day))
                dict[day] = 0;
            dict[day] += VietnameseMoneyParser.ParseDecimalOrZero(r.TotalAmount);
        }

        return dict.OrderBy(x => x.Key)
            .Select(x => new ChartPointDto(x.Key.ToString("dd/MM"), x.Value))
            .ToList();
    }

    private static IReadOnlyList<ChartPointDto> BuildWeekdaySeries(IReadOnlyList<SaleRow> normalSales)
    {
        // Thứ 2 = index 0 ... Chủ nhật = index 6
        var labels = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN" };
        var sums = new decimal[7];
        foreach (var r in normalSales)
        {
            var vn = DashboardDateRange.ToVietnamWallFromBillUtc(r.BillCreatedAt);
            var idx = vn.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)vn.DayOfWeek - 1;
            sums[idx] += VietnameseMoneyParser.ParseDecimalOrZero(r.TotalAmount);
        }

        return Enumerable.Range(0, 7)
            .Select(i => new ChartPointDto(labels[i], sums[i]))
            .ToList();
    }
}
