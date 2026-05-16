using Microsoft.EntityFrameworkCore;
using Quay27.Application.Orders;
using Quay27.Application.Reports;
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

    public async Task<IReadOnlyList<EndOfDaySalesRowDto>> ListForEndOfDayReportAsync(
        EndOfDayReportQuery query,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var q = _db.SalesInvoices.AsNoTracking()
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(query.CustomerSearch))
        {
            var s = query.CustomerSearch.Trim();
            q = q.Where(x =>
                (x.CustomerCode != null && x.CustomerCode.Contains(s)) ||
                (x.CustomerName != null && x.CustomerName.Contains(s)) ||
                x.Code.Contains(s));
        }

        if (query.SellerUserId.HasValue)
            q = q.Where(x => x.SellerUserId == query.SellerUserId.Value);
        if (query.CreatedByUserId.HasValue)
            q = q.Where(x => x.CreatedByUserId == query.CreatedByUserId.Value);
        if (!string.IsNullOrWhiteSpace(query.PaymentMethod))
            q = q.Where(x => x.PaymentMethod == query.PaymentMethod.Trim());
        if (query.SaleChannelId.HasValue)
            q = q.Where(x => x.SaleChannelId == query.SaleChannelId.Value);

        return await (
            from x in q
            orderby x.CreatedAtUtc
            join seller in _db.Users.AsNoTracking() on x.SellerUserId equals seller.Id into sellers
            from seller in sellers.DefaultIfEmpty()
            select new EndOfDaySalesRowDto
            {
                Code = x.Code,
                CreatedAtUtc = x.CreatedAtUtc,
                CustomerName = x.CustomerName,
                SellerDisplayName = seller == null
                    ? null
                    : (string.IsNullOrWhiteSpace(seller.FullName) ? seller.Username : seller.FullName),
                Quantity = x.Items.Sum(i => i.Quantity),
                SubtotalAmount = x.SubtotalAmount,
                DiscountAmount = x.DiscountAmount,
                PaidAmount = x.PaidAmount,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EndOfDayProductRowDto>> ListForEndOfDayProductsAsync(
        EndOfDayReportQuery query,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var invoiceQ = _db.SalesInvoices.AsNoTracking()
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(query.CustomerSearch))
        {
            var s = query.CustomerSearch.Trim();
            invoiceQ = invoiceQ.Where(x =>
                (x.CustomerCode != null && x.CustomerCode.Contains(s)) ||
                (x.CustomerName != null && x.CustomerName.Contains(s)) ||
                x.Code.Contains(s));
        }

        if (query.SellerUserId.HasValue)
            invoiceQ = invoiceQ.Where(x => x.SellerUserId == query.SellerUserId.Value);

        var returnQ = _db.SalesReturns.AsNoTracking()
            .Where(x => x.Status == "returned" && x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc);

        if (query.SellerUserId.HasValue)
            returnQ = returnQ.Where(x => x.SellerUserId == query.SellerUserId.Value);

        if (!string.IsNullOrWhiteSpace(query.CustomerSearch))
        {
            var s = query.CustomerSearch.Trim();
            returnQ = returnQ.Where(x =>
                (x.CustomerCode != null && x.CustomerCode.Contains(s)) ||
                (x.CustomerName != null && x.CustomerName.Contains(s)) ||
                x.Code.Contains(s));
        }

        var salesLines = from item in _db.SalesInvoiceItems.AsNoTracking()
            join inv in invoiceQ on item.SalesInvoiceId equals inv.Id
            join product in _db.Products.AsNoTracking() on item.ProductId equals product.Id
            where !product.IsDeleted
            select new
            {
                item.ProductId,
                item.ProductCode,
                item.ProductName,
                item.Quantity,
                item.LineTotal,
                item.UnitPrice,
                product.ItemType,
                product.GroupId,
                product.SalePrice,
            };

        if (!string.IsNullOrWhiteSpace(query.ProductSearch))
        {
            var ps = query.ProductSearch.Trim();
            salesLines = salesLines.Where(x =>
                x.ProductCode.Contains(ps) || x.ProductName.Contains(ps));
        }

        if (!string.IsNullOrWhiteSpace(query.ProductType))
        {
            var pt = query.ProductType.Trim();
            salesLines = salesLines.Where(x => x.ItemType == pt);
        }

        if (query.ProductGroupId.HasValue)
            salesLines = salesLines.Where(x => x.GroupId == query.ProductGroupId.Value);

        var returnLines = from item in _db.SalesReturnItems.AsNoTracking()
            join ret in returnQ on item.SalesReturnId equals ret.Id
            join product in _db.Products.AsNoTracking() on item.ProductId equals product.Id
            where !product.IsDeleted
            select new
            {
                item.ProductId,
                item.Quantity,
                item.LineTotal,
                product.ItemType,
                product.GroupId,
                item.ProductCode,
                item.ProductName,
            };

        if (!string.IsNullOrWhiteSpace(query.ProductSearch))
        {
            var ps = query.ProductSearch.Trim();
            returnLines = returnLines.Where(x =>
                x.ProductCode.Contains(ps) || x.ProductName.Contains(ps));
        }

        if (!string.IsNullOrWhiteSpace(query.ProductType))
        {
            var pt = query.ProductType.Trim();
            returnLines = returnLines.Where(x => x.ItemType == pt);
        }

        if (query.ProductGroupId.HasValue)
            returnLines = returnLines.Where(x => x.GroupId == query.ProductGroupId.Value);

        var salesList = await salesLines.ToListAsync(cancellationToken);
        var returnList = await returnLines.ToListAsync(cancellationToken);

        var returnsByProduct = returnList
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => (Qty: g.Sum(x => x.Quantity), Value: g.Sum(x => x.LineTotal)));

        if (query.GroupSameProducts)
        {
            return salesList
                .GroupBy(x => x.ProductId)
                .Select(g =>
                {
                    var returnQty = 0;
                    var returnValue = 0m;
                    if (returnsByProduct.TryGetValue(g.Key, out var ret))
                    {
                        returnQty = ret.Qty;
                        returnValue = ret.Value;
                    }

                    var revenue = g.Sum(x => x.LineTotal);
                    var listPrice = g.First().SalePrice;
                    return new EndOfDayProductRowDto
                    {
                        ProductCode = g.First().ProductCode,
                        ProductName = g.First().ProductName,
                        SoldQuantity = g.Sum(x => x.Quantity),
                        Revenue = revenue,
                        ReturnQuantity = returnQty,
                        ReturnValue = returnValue,
                        NetRevenue = revenue - returnValue,
                        ListPrice = listPrice,
                        Variance = revenue - g.Sum(x => x.UnitPrice * x.Quantity),
                    };
                })
                .OrderBy(x => x.ProductCode)
                .ToList();
        }

        return salesList
            .Select(x =>
            {
                returnsByProduct.TryGetValue(x.ProductId, out var ret);
                return new EndOfDayProductRowDto
                {
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,
                    SoldQuantity = x.Quantity,
                    Revenue = x.LineTotal,
                    ReturnQuantity = 0,
                    ReturnValue = 0,
                    NetRevenue = x.LineTotal,
                    ListPrice = x.SalePrice,
                    Variance = x.LineTotal - x.UnitPrice * x.Quantity,
                };
            })
            .OrderBy(x => x.ProductCode)
            .ToList();
    }

    public async Task<(IReadOnlyList<EndOfDaySalesSummaryRowDto> ValueRows, IReadOnlyList<EndOfDaySalesCountRowDto> CountRows)>
        GetSalesSummaryForEndOfDayAsync(
            EndOfDayReportQuery query,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default)
    {
        var q = _db.SalesInvoices.AsNoTracking()
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc);

        if (query.SellerUserId.HasValue)
            q = q.Where(x => x.SellerUserId == query.SellerUserId.Value);
        if (query.CreatedByUserId.HasValue)
            q = q.Where(x => x.CreatedByUserId == query.CreatedByUserId.Value);

        var invoices = await q
            .Select(x => new { x.PaymentMethod, x.PaidAmount, x.SubtotalAmount })
            .ToListAsync(cancellationToken);

        static (decimal cash, decimal transfer, decimal card) SplitPayment(string method, decimal amount) =>
            method switch
            {
                "cash" => (amount, 0, 0),
                "transfer" => (0, amount, 0),
                _ => (0, 0, amount),
            };

        decimal paidCash = 0, paidTransfer = 0, paidCard = 0;
        decimal subCash = 0, subTransfer = 0, subCard = 0;
        var countCash = 0;
        var countTransfer = 0;

        foreach (var inv in invoices)
        {
            var (pc, pt, pk) = SplitPayment(inv.PaymentMethod, inv.PaidAmount);
            paidCash += pc;
            paidTransfer += pt;
            paidCard += pk;

            var (sc, st, sk) = SplitPayment(inv.PaymentMethod, inv.SubtotalAmount);
            subCash += sc;
            subTransfer += st;
            subCard += sk;

            if (inv.PaymentMethod == "cash")
                countCash++;
            else if (inv.PaymentMethod == "transfer")
                countTransfer++;
        }

        var valueRows = new List<EndOfDaySalesSummaryRowDto>
        {
            new()
            {
                Label = "Bán hàng",
                Value = invoices.Sum(x => x.SubtotalAmount),
                CashAmount = subCash,
                TransferAmount = subTransfer,
                CardAmount = subCard,
            },
            new()
            {
                Label = "Thực thu",
                Value = invoices.Sum(x => x.PaidAmount),
                CashAmount = paidCash,
                TransferAmount = paidTransfer,
                CardAmount = paidCard,
            },
        };

        var countRows = new List<EndOfDaySalesCountRowDto>
        {
            new()
            {
                Label = "Bán hàng",
                TransactionCount = invoices.Count,
                CashAmount = countCash,
                TransferAmount = countTransfer,
            },
        };

        return (valueRows, countRows);
    }

    public Task AddAsync(SalesInvoice entity, CancellationToken cancellationToken = default) =>
        _db.SalesInvoices.AddAsync(entity, cancellationToken).AsTask();

    public Task<SalesInvoice?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SalesInvoices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SalesInvoice?> GetByIdNoTrackingAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SalesInvoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<SalesInvoiceDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.SalesInvoices.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.CreatedByUser)
            .Include(x => x.SellerUser)
            .Include(x => x.SaleChannel)
            .Include(x => x.PriceList)
            .Include(x => x.PurchaseOrder)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null)
            return null;

        var lines = invoice.Items
            .OrderBy(x => x.ProductCode)
            .Select(line =>
            {
                var gross = line.Quantity * line.UnitPrice;
                var discount = gross > line.LineTotal ? gross - line.LineTotal : 0m;
                return new SalesInvoiceLineDto(
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

        return new SalesInvoiceDetailDto(
            invoice.Id,
            invoice.Code,
            invoice.Status,
            invoice.CustomerCode,
            invoice.CustomerName,
            invoice.CreatedAtUtc,
            UserDisplayName(invoice.CreatedByUser),
            UserDisplayName(invoice.SellerUser),
            invoice.SaleChannel?.Name,
            invoice.PriceList?.Name,
            invoice.PurchaseOrderId,
            invoice.PurchaseOrder?.Code,
            invoice.Note,
            invoice.SubtotalAmount,
            invoice.DiscountAmount,
            invoice.SubtotalAmount - invoice.DiscountAmount,
            invoice.PaidAmount,
            "Chi nhánh trung tâm",
            lines);
    }

    public async Task<IReadOnlyList<SalesInvoiceCashbookRowDto>> ListCashbookEntriesAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _db.SalesInvoices.AsNoTracking()
            .Where(x => x.Id == invoiceId)
            .Select(x => new { x.Id, x.PurchaseOrderId })
            .FirstOrDefaultAsync(cancellationToken);
        if (invoice is null)
            return Array.Empty<SalesInvoiceCashbookRowDto>();

        var poId = invoice.PurchaseOrderId;

        return await (
            from e in _db.CashbookEntries.AsNoTracking()
            where (e.SourceKind == "SalesInvoice" && e.SourceId == invoiceId)
                  || (poId.HasValue && e.SourceKind == "PurchaseOrder" && e.SourceId == poId.Value)
            orderby e.OccurredAtUtc descending
            join u in _db.Users.AsNoTracking() on e.CreatedByUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new SalesInvoiceCashbookRowDto(
                e.Id,
                e.Code,
                e.SourceKind == "PurchaseOrder" ? "(Chuyển tạm ứng)" : e.Code,
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
    }

    public async Task<IReadOnlyList<SalesInvoiceReturnRowDto>> ListReturnsAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var refCode = await _db.SalesInvoices.AsNoTracking()
            .Where(x => x.Id == invoiceId)
            .Select(x => x.ReturnReferenceCode)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(refCode))
            return Array.Empty<SalesInvoiceReturnRowDto>();

        return await (
            from r in _db.SalesReturns.AsNoTracking()
            where r.Code == refCode
            orderby r.CreatedAtUtc descending
            join u in _db.Users.AsNoTracking() on r.ReceivedByUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new SalesInvoiceReturnRowDto(
                r.Id,
                r.Code,
                r.CreatedAtUtc,
                u != null
                    ? (string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName)
                    : null,
                r.ReturnSubtotalAmount - r.ReturnDiscountAmount + r.ReturnFeeAmount,
                r.Status))
            .ToListAsync(cancellationToken);
    }

    private static string? UserDisplayName(User? user) =>
        user is null
            ? null
            : string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName.Trim();

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
