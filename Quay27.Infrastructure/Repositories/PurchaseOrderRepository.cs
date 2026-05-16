using Microsoft.EntityFrameworkCore;
using Quay27.Application.Orders;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly ApplicationDbContext _db;

    public PurchaseOrderRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.PurchaseOrders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.Code.Contains(s) ||
                (x.CustomerCode != null && x.CustomerCode.Contains(s)) ||
                (x.CustomerName != null && x.CustomerName.Contains(s)));
        }

        if (query.Statuses is { Count: > 0 })
            q = q.Where(x => query.Statuses.Contains(x.Status));

        if (query.FromUtc.HasValue) q = q.Where(x => x.CreatedAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue) q = q.Where(x => x.CreatedAtUtc <= query.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(query.DeliveryPartnerContains))
        {
            var d = query.DeliveryPartnerContains.Trim();
            q = q.Where(x => x.DeliveryPartner != null && x.DeliveryPartner.Contains(d));
        }

        if (query.DeliveryFromUtc.HasValue || query.DeliveryToUtc.HasValue)
        {
            var df = query.DeliveryFromUtc;
            var dt = query.DeliveryToUtc;
            q = q.Where(x =>
                (!df.HasValue || (x.DeliveryToUtc ?? x.DeliveryFromUtc ?? x.CreatedAtUtc) >= df) &&
                (!dt.HasValue || (x.DeliveryFromUtc ?? x.CreatedAtUtc) <= dt));
        }

        if (query.ProvinceKeys is { Count: > 0 }) q = q.Where(x => x.ProvinceKey != null && query.ProvinceKeys.Contains(x.ProvinceKey));
        if (query.PaymentMethods is { Count: > 0 })
            q = q.Where(x => query.PaymentMethods.Contains(x.PaymentMethod));
        if (query.CreatedByUserIds is { Count: > 0 })
            q = q.Where(x => x.CreatedByUserId.HasValue && query.CreatedByUserIds.Contains(x.CreatedByUserId.Value));
        if (query.ReceivedByUserIds is { Count: > 0 })
            q = q.Where(x => x.ReceivedByUserId.HasValue && query.ReceivedByUserIds.Contains(x.ReceivedByUserId.Value));
        if (query.SaleChannelIds is { Count: > 0 })
            q = q.Where(x => x.SaleChannelId.HasValue && query.SaleChannelIds.Contains(x.SaleChannelId.Value));

        return await q.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PurchaseOrderListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                CreatedAtUtc = x.CreatedAtUtc,
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                Status = x.Status,
                AmountDue = x.AmountDue,
                AmountPaid = x.AmountPaid,
            })
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(PurchaseOrder entity, CancellationToken cancellationToken = default) =>
        _db.PurchaseOrders.AddAsync(entity, cancellationToken).AsTask();

    public Task<PurchaseOrder?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PurchaseOrder?> GetByIdNoTrackingAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "DH";
        var maxCode = await _db.PurchaseOrders
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

    public async Task<int> SumReservedQuantityForProductInOpenOrdersAsync(Guid productId,
        CancellationToken cancellationToken = default)
    {
        var openStatuses = new[] { "draft", "confirmed", "shipping" };
        var sum = await _db.PurchaseOrderItems
            .AsNoTracking()
            .Where(i => i.ProductId == productId)
            .Join(
                _db.PurchaseOrders.AsNoTracking(),
                item => item.PurchaseOrderId,
                order => order.Id,
                (item, order) => new { item.Quantity, order.Status })
            .Where(x => openStatuses.Contains(x.Status))
            .SumAsync(x => (int?)x.Quantity, cancellationToken);
        return sum ?? 0;
    }

    public async Task<PurchaseOrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.CreatedByUser)
            .Include(x => x.ReceivedByUser)
            .Include(x => x.SellerUser)
            .Include(x => x.SaleChannel)
            .Include(x => x.PriceList)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null)
            return null;

        var fulfilledByProduct = await (
            from item in _db.SalesInvoiceItems.AsNoTracking()
            join inv in _db.SalesInvoices.AsNoTracking() on item.SalesInvoiceId equals inv.Id
            where inv.PurchaseOrderId == id
            group item by item.ProductId
            into g
            select new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, cancellationToken);

        var primaryInvoice = await _db.SalesInvoices.AsNoTracking()
            .Where(x => x.PurchaseOrderId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Code })
            .FirstOrDefaultAsync(cancellationToken);

        var lines = order.Items
            .OrderBy(x => x.ProductCode)
            .Select(line =>
            {
                fulfilledByProduct.TryGetValue(line.ProductId, out var fulfilled);
                var gross = line.Quantity * line.UnitPrice;
                var discount = gross > line.LineTotal ? gross - line.LineTotal : 0m;
                return new PurchaseOrderLineDto(
                    line.Id,
                    line.ProductCode,
                    line.ProductName,
                    line.Quantity,
                    fulfilled,
                    line.UnitPrice,
                    discount,
                    line.UnitPrice,
                    line.LineTotal,
                    line.Note);
            })
            .ToList();

        return new PurchaseOrderDetailDto(
            order.Id,
            order.Code,
            order.Status,
            order.CustomerCode,
            order.CustomerName,
            order.CreatedAtUtc,
            UserDisplayName(order.CreatedByUser),
            UserDisplayName(order.ReceivedByUser),
            UserDisplayName(order.SellerUser),
            order.SaleChannel?.Name,
            order.PriceList?.Name,
            primaryInvoice?.Id,
            primaryInvoice?.Code,
            FormatExpectedDelivery(order.DeliveryFromUtc, order.DeliveryToUtc),
            order.Note,
            order.SubtotalAmount,
            order.DiscountAmount,
            order.AmountDue,
            order.AmountPaid,
            "Chi nhánh trung tâm",
            lines);
    }

    public async Task<IReadOnlyList<PurchaseOrderLinkedInvoiceDto>> ListLinkedInvoicesAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default) =>
        await (
            from inv in _db.SalesInvoices.AsNoTracking()
            where inv.PurchaseOrderId == purchaseOrderId
            orderby inv.CreatedAtUtc descending
            join u in _db.Users.AsNoTracking() on inv.CreatedByUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new PurchaseOrderLinkedInvoiceDto(
                inv.Id,
                inv.Code,
                inv.CreatedAtUtc,
                u != null
                    ? (string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName)
                    : null,
                inv.SubtotalAmount - inv.DiscountAmount,
                inv.Status))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PurchaseOrderCashbookRowDto>> ListCashbookEntriesAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var invoiceIds = await _db.SalesInvoices.AsNoTracking()
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return await (
            from e in _db.CashbookEntries.AsNoTracking()
            where (e.SourceKind == "PurchaseOrder" && e.SourceId == purchaseOrderId)
                  || (e.SourceKind == "SalesInvoice" && e.SourceId.HasValue && invoiceIds.Contains(e.SourceId.Value))
            orderby e.OccurredAtUtc descending
            join u in _db.Users.AsNoTracking() on e.CreatedByUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new PurchaseOrderCashbookRowDto(
                e.Id,
                e.Code,
                e.OccurredAtUtc,
                u != null
                    ? (string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName)
                    : null,
                e.FundType,
                e.Status,
                e.Amount,
                e.EntryType))
            .ToListAsync(cancellationToken);
    }

    private static string? UserDisplayName(User? user) =>
        user is null
            ? null
            : string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName.Trim();

    private static string? FormatExpectedDelivery(DateTime? from, DateTime? to)
    {
        if (!from.HasValue && !to.HasValue)
            return null;
        if (from.HasValue && to.HasValue && from.Value.Date != to.Value.Date)
            return $"{from.Value:dd/MM/yyyy} – {to.Value:dd/MM/yyyy}";
        var d = from ?? to;
        return d?.ToString("dd/MM/yyyy");
    }
}
