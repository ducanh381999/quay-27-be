using Microsoft.EntityFrameworkCore;
using Quay27.Application.Orders;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;
using UserEntity = Quay27.Domain.Entities.User;

namespace Quay27.Infrastructure.Repositories;

public sealed class SalesReturnRepository : ISalesReturnRepository
{
    private readonly ApplicationDbContext _db;

    public SalesReturnRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalesReturnListItemDto>> ListAsync(SalesReturnListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.SalesReturns.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.Code.Contains(s) ||
                (x.CustomerCode != null && x.CustomerCode.Contains(s)) ||
                (x.CustomerName != null && x.CustomerName.Contains(s)));
        }

        if (query.ReturnTypes is { Count: > 0 })
            q = q.Where(x => query.ReturnTypes.Contains(x.ReturnType));
        if (query.Statuses is { Count: > 0 })
            q = q.Where(x => query.Statuses.Contains(x.Status));

        if (query.FromUtc.HasValue) q = q.Where(x => x.CreatedAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue) q = q.Where(x => x.CreatedAtUtc <= query.ToUtc.Value);

        if (query.CreatedByUserIds is { Count: > 0 })
            q = q.Where(x => x.CreatedByUserId.HasValue && query.CreatedByUserIds.Contains(x.CreatedByUserId.Value));
        if (query.ReceivedByUserIds is { Count: > 0 })
            q = q.Where(x => x.ReceivedByUserId.HasValue && query.ReceivedByUserIds.Contains(x.ReceivedByUserId.Value));
        if (query.SaleChannelIds is { Count: > 0 })
            q = q.Where(x => x.SaleChannelId.HasValue && query.SaleChannelIds.Contains(x.SaleChannelId.Value));
        if (query.OtherCollectionTypes is { Count: > 0 })
            q = q.Where(x => x.OtherCollectionType != null && query.OtherCollectionTypes.Contains(x.OtherCollectionType));

        return await q.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new SalesReturnListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                CreatedAtUtc = x.CreatedAtUtc,
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                Status = x.Status,
                ReturnType = x.ReturnType,
                Amount = x.Amount,
            })
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(SalesReturn entity, CancellationToken cancellationToken = default) =>
        _db.SalesReturns.AddAsync(entity, cancellationToken).AsTask();

    public Task<SalesReturn?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SalesReturns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SalesReturn?> GetByIdNoTrackingAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SalesReturns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "TH";
        var maxCode = await _db.SalesReturns
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(prefix))
            .Select(x => x.Code)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length <= prefix.Length ||
            !int.TryParse(maxCode[prefix.Length..], out var number))
            return $"{prefix}000001";

        return $"{prefix}{(number + 1).ToString().PadLeft(6, '0')}";
    }

    public async Task<SalesReturnDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _db.SalesReturns.AsNoTracking()
            .Include(x => x.ReturnItems)
            .Include(x => x.CreatedByUser)
            .Include(x => x.ReceivedByUser)
            .Include(x => x.SaleChannel)
            .Include(x => x.PriceList)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null)
            return null;

        var invoice = await _db.SalesInvoices.AsNoTracking()
            .Where(x => x.ReturnReferenceCode == order.Code)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Code })
            .FirstOrDefaultAsync(cancellationToken);

        var paymentVoucher = await _db.CashbookEntries.AsNoTracking()
            .Where(x => x.SourceKind == "SalesReturn" && x.SourceId == id)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new { x.Id, x.Code })
            .FirstOrDefaultAsync(cancellationToken);

        var lines = order.ReturnItems
            .OrderBy(x => x.ProductCode)
            .Select(line =>
            {
                var gross = line.Quantity * line.UnitPrice;
                var discount = gross > line.LineTotal ? gross - line.LineTotal : 0m;
                return new SalesReturnLineDto(
                    line.Id,
                    line.ProductCode,
                    line.ProductName,
                    line.Quantity,
                    line.UnitPrice,
                    discount,
                    line.UnitPrice,
                    line.LineTotal,
                    line.Note);
            })
            .ToList();

        return new SalesReturnDetailDto(
            order.Id,
            order.Code,
            order.Status,
            order.CustomerCode,
            order.CustomerName,
            order.CreatedAtUtc,
            UserDisplayName(order.CreatedByUser),
            UserDisplayName(order.ReceivedByUser),
            order.ReceivedByUserId,
            order.SaleChannel?.Name,
            order.PriceList?.Name,
            order.Note,
            invoice?.Id,
            invoice?.Code,
            paymentVoucher?.Id,
            paymentVoucher?.Code,
            order.ReturnSubtotalAmount,
            order.ReturnDiscountAmount,
            order.ReturnFeeAmount,
            order.RefundDueAmount,
            order.PaidAmount,
            "Chi nhánh trung tâm",
            lines);
    }

    public async Task<IReadOnlyList<SalesReturnCashbookRowDto>> ListCashbookEntriesAsync(
        Guid returnId,
        CancellationToken cancellationToken = default) =>
        await (
            from e in _db.CashbookEntries.AsNoTracking()
            where e.SourceKind == "SalesReturn" && e.SourceId == returnId
            orderby e.OccurredAtUtc descending
            join u in _db.Users.AsNoTracking() on e.CreatedByUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new SalesReturnCashbookRowDto(
                e.Id,
                e.Code,
                e.Code,
                e.OccurredAtUtc,
                u != null
                    ? (string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName)
                    : null,
                e.FundType,
                e.Status,
                e.Amount,
                e.Amount,
                e.EntryType))
            .ToListAsync(cancellationToken);

    private static string? UserDisplayName(UserEntity? user) =>
        user is null
            ? null
            : string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName.Trim();
}
