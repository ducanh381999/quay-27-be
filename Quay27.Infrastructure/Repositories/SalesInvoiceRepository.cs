using Microsoft.EntityFrameworkCore;
using Quay27.Application.Orders;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class SalesInvoiceRepository : ISalesInvoiceRepository
{
    private readonly ApplicationDbContext _db;

    public SalesInvoiceRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalesInvoiceListItemDto>> ListAsync(SalesInvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.SalesInvoices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.Code.Contains(s) ||
                (x.ReturnReferenceCode != null && x.ReturnReferenceCode.Contains(s)) ||
                (x.CustomerCode != null && x.CustomerCode.Contains(s)) ||
                (x.CustomerName != null && x.CustomerName.Contains(s)));
        }

        if (query.InvoiceDeliveryTypes is { Count: > 0 })
            q = q.Where(x => query.InvoiceDeliveryTypes.Contains(x.InvoiceDeliveryType));
        if (query.Statuses is { Count: > 0 })
            q = q.Where(x => query.Statuses.Contains(x.Status));
        if (query.DeliveryStatuses is { Count: > 0 })
            q = q.Where(x => x.DeliveryStatus != null && query.DeliveryStatuses.Contains(x.DeliveryStatus));

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

        if (query.ProvinceKeys is { Count: > 0 })
            q = q.Where(x => x.ProvinceKey != null && query.ProvinceKeys.Contains(x.ProvinceKey));
        if (query.PaymentMethods is { Count: > 0 })
            q = q.Where(x => query.PaymentMethods.Contains(x.PaymentMethod));
        if (query.CreatedByUserIds is { Count: > 0 })
            q = q.Where(x => x.CreatedByUserId.HasValue && query.CreatedByUserIds.Contains(x.CreatedByUserId.Value));
        if (query.SellerUserIds is { Count: > 0 })
            q = q.Where(x => x.SellerUserId.HasValue && query.SellerUserIds.Contains(x.SellerUserId.Value));
        if (query.PriceListIds is { Count: > 0 })
            q = q.Where(x => x.PriceListId.HasValue && query.PriceListIds.Contains(x.PriceListId.Value));
        if (query.SaleChannelIds is { Count: > 0 })
            q = q.Where(x => x.SaleChannelId.HasValue && query.SaleChannelIds.Contains(x.SaleChannelId.Value));

        return await q.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new SalesInvoiceListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                CreatedAtUtc = x.CreatedAtUtc,
                ReturnReferenceCode = x.ReturnReferenceCode,
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                SubtotalAmount = x.SubtotalAmount,
                DiscountAmount = x.DiscountAmount,
                PaidAmount = x.PaidAmount,
            })
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(SalesInvoice entity, CancellationToken cancellationToken = default) =>
        _db.SalesInvoices.AddAsync(entity, cancellationToken).AsTask();

    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "HD";
        var maxCode = await _db.SalesInvoices
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
}
