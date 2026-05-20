using Quay27.Application.CustomerProfiles;
using Quay27.Application.Orders;
using Quay27.Application.Products;
using Quay27.Application.Suppliers;

namespace Quay27.Application.Reports;

public static class ReportQueryMapper
{
    public static (DateTime FromUtc, DateTime ToUtc, string DateLabel) ResolveRange(EndOfDayReportQuery query) =>
        EndOfDayReportTimeRange.Resolve(query);

    public static SalesInvoiceListQuery ToSalesInvoiceListQuery(EndOfDayReportQuery query, DateTime fromUtc, DateTime toUtc)
    {
        var paymentMethods = string.IsNullOrWhiteSpace(query.PaymentMethod)
            ? null
            : new[] { query.PaymentMethod.Trim() };
        var sellerIds = query.SellerUserId.HasValue ? new[] { query.SellerUserId.Value } : null;
        var createdByIds = query.CreatedByUserId.HasValue ? new[] { query.CreatedByUserId.Value } : null;
        var saleChannelIds = query.SaleChannelId.HasValue ? new[] { query.SaleChannelId.Value } : null;

        return new SalesInvoiceListQuery(
            query.CustomerSearch,
            null,
            null,
            null,
            fromUtc,
            toUtc,
            null,
            null,
            null,
            null,
            paymentMethods,
            createdByIds,
            sellerIds,
            null,
            saleChannelIds);
    }

    public static PurchaseOrderListQuery ToPurchaseOrderListQuery(EndOfDayReportQuery query, DateTime fromUtc, DateTime toUtc)
    {
        var paymentMethods = string.IsNullOrWhiteSpace(query.PaymentMethod)
            ? null
            : new[] { query.PaymentMethod.Trim() };
        var createdByIds = query.CreatedByUserId.HasValue ? new[] { query.CreatedByUserId.Value } : null;
        var receivedByIds = query.SellerUserId.HasValue ? new[] { query.SellerUserId.Value } : null;
        var saleChannelIds = query.SaleChannelId.HasValue ? new[] { query.SaleChannelId.Value } : null;

        return new PurchaseOrderListQuery(
            query.CustomerSearch,
            null,
            fromUtc,
            toUtc,
            null,
            null,
            null,
            null,
            paymentMethods,
            createdByIds,
            receivedByIds,
            saleChannelIds);
    }

    public static ProductQuery ToProductQuery(EndOfDayReportQuery query) =>
        new()
        {
            Search = query.CustomerSearch,
            Page = 1,
            PageSize = 10_000,
        };

    public static CustomerProfileListQuery ToCustomerProfileListQuery(EndOfDayReportQuery query) =>
        new()
        {
            Search = query.CustomerSearch,
            Take = 10_000,
            Skip = 0,
            Status = "all",
        };

    public static ListSuppliersQuery ToSuppliersQuery(EndOfDayReportQuery query) =>
        new(
            Search: query.CustomerSearch,
            Status: "all");
}
