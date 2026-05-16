using Quay27.Application.Abstractions;
using Quay27.Application.Reports;
using Quay27.Application.Repositories;

namespace Quay27.Infrastructure.Services;

public sealed class ReportsService : IReportsService
{
    private readonly ISalesInvoiceRepository _invoices;
    private readonly ICashbookRepository _cashbook;
    private readonly ISalesInvoiceService _salesInvoices;
    private readonly IPurchaseOrderService _purchaseOrders;
    private readonly IProductService _products;
    private readonly ICustomerProfileService _customerProfiles;
    private readonly ISupplierService _suppliers;

    public ReportsService(
        ISalesInvoiceRepository invoices,
        ICashbookRepository cashbook,
        ISalesInvoiceService salesInvoices,
        IPurchaseOrderService purchaseOrders,
        IProductService products,
        ICustomerProfileService customerProfiles,
        ISupplierService suppliers)
    {
        _invoices = invoices;
        _cashbook = cashbook;
        _salesInvoices = salesInvoices;
        _purchaseOrders = purchaseOrders;
        _products = products;
        _customerProfiles = customerProfiles;
        _suppliers = suppliers;
    }

    public async Task<ReportDataDto<EndOfDayUnifiedRowApiDto>> GetEndOfDayDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateEndOfDayConcern(query);
        var range = ReportQueryMapper.ResolveRange(query);
        var concern = query.Concern.Trim().ToLowerInvariant();

        IReadOnlyList<EndOfDayUnifiedRowApiDto> rows;
        string title;
        string displayMode = query.DisplayMode;

        switch (concern)
        {
            case "cashflow":
                title = "Báo cáo cuối ngày về thu chi";
                if (IsHorizontal(query.DisplayMode))
                {
                    var detail = await _cashbook.ListForEndOfDayReportAsync(
                        query, range.FromUtc, range.ToUtc, cancellationToken);
                    rows = detail.Select(MapCashflowDetailRow).ToList();
                }
                else
                {
                    var agg = await _cashbook.AggregateForEndOfDayReportAsync(
                        query, range.FromUtc, range.ToUtc, cancellationToken);
                    rows = agg.Select(MapCashflowAggregateRow).ToList();
                }

                break;
            case "products":
                title = "Báo cáo cuối ngày về hàng hóa";
                var products = await _invoices.ListForEndOfDayProductsAsync(
                    query, range.FromUtc, range.ToUtc, cancellationToken);
                rows = products.Select(MapProductRow).ToList();
                break;
            case "summary":
                title = "Báo cáo cuối ngày tổng hợp";
                displayMode = "vertical";
                rows = await BuildSummaryGridRowsAsync(query, range, cancellationToken);
                break;
            default:
                title = "Báo cáo cuối ngày về bán hàng";
                var sales = await _invoices.ListForEndOfDayReportAsync(
                    query, range.FromUtc, range.ToUtc, cancellationToken);
                rows = sales.Select(MapSalesRow).ToList();
                break;
        }

        return new ReportDataDto<EndOfDayUnifiedRowApiDto>
        {
            Meta = BuildMeta(title, range.DateLabel, displayMode),
            Rows = rows,
        };
    }

    public Task<byte[]> ExportEndOfDayExcelAsync(EndOfDayReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateEndOfDayConcern(query);
        var range = ReportQueryMapper.ResolveRange(query);
        return ExportEndOfDayExcelCoreAsync(query, range, cancellationToken);
    }

    public async Task<ReportDataDto<SalesReportRowApiDto>> GetSalesDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var range = ReportQueryMapper.ResolveRange(query);
        var listQuery = ReportQueryMapper.ToSalesInvoiceListQuery(query, range.FromUtc, range.ToUtc);
        var items = await _salesInvoices.ListAsync(listQuery, cancellationToken);
        return new ReportDataDto<SalesReportRowApiDto>
        {
            Meta = BuildMeta("Báo cáo bán hàng", range.DateLabel, query.DisplayMode),
            Rows = items.Select(x => new SalesReportRowApiDto
            {
                Code = x.Code,
                CreatedAtUtc = x.CreatedAtUtc,
                CustomerName = x.CustomerName,
                SubtotalAmount = x.SubtotalAmount,
                DiscountAmount = x.DiscountAmount,
                PaidAmount = x.PaidAmount,
            }).ToList(),
        };
    }

    public async Task<byte[]> ExportSalesExcelAsync(EndOfDayReportQuery query, CancellationToken cancellationToken = default)
    {
        var data = await GetSalesDataAsync(query, cancellationToken);
        var rows = data.Rows.Select(r => (IReadOnlyList<object?>)new object?[]
        {
            r.Code,
            ToLocal(r.CreatedAtUtc),
            r.CustomerName ?? "",
            r.SubtotalAmount,
            r.DiscountAmount,
            r.PaidAmount,
        }).ToList();
        return GenericListReportExcelFiller.Fill(
            ReportTemplatePaths.Resolve("SalesReport.xlsx"),
            data.Meta.Title,
            data.Meta,
            ["Mã HĐ", "Thời gian", "Khách hàng", "Doanh thu", "Giảm giá", "Đã thu"],
            rows);
    }

    public async Task<ReportDataDto<OrdersReportRowApiDto>> GetOrdersDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var range = ReportQueryMapper.ResolveRange(query);
        var listQuery = ReportQueryMapper.ToPurchaseOrderListQuery(query, range.FromUtc, range.ToUtc);
        var items = await _purchaseOrders.ListAsync(listQuery, cancellationToken);
        return new ReportDataDto<OrdersReportRowApiDto>
        {
            Meta = BuildMeta("Báo cáo đặt hàng", range.DateLabel, query.DisplayMode),
            Rows = items.Select(x => new OrdersReportRowApiDto
            {
                Code = x.Code,
                CreatedAtUtc = x.CreatedAtUtc,
                CustomerName = x.CustomerName,
                Status = x.Status,
                AmountDue = x.AmountDue,
                AmountPaid = x.AmountPaid,
            }).ToList(),
        };
    }

    public async Task<byte[]> ExportOrdersExcelAsync(EndOfDayReportQuery query, CancellationToken cancellationToken = default)
    {
        var data = await GetOrdersDataAsync(query, cancellationToken);
        var rows = data.Rows.Select(r => (IReadOnlyList<object?>)new object?[]
        {
            r.Code,
            ToLocal(r.CreatedAtUtc),
            r.CustomerName ?? "",
            r.Status,
            r.AmountDue,
            r.AmountPaid,
        }).ToList();
        return GenericListReportExcelFiller.Fill(
            ReportTemplatePaths.Resolve("OrdersReport.xlsx"),
            data.Meta.Title,
            data.Meta,
            ["Mã đặt hàng", "Thời gian", "Khách hàng", "Trạng thái", "Phải thu", "Đã thu"],
            rows);
    }

    public async Task<ReportDataDto<ProductsReportRowApiDto>> GetProductsDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var range = ReportQueryMapper.ResolveRange(query);
        var response = await _products.ListAsync(ReportQueryMapper.ToProductQuery(query), cancellationToken);
        return new ReportDataDto<ProductsReportRowApiDto>
        {
            Meta = BuildMeta("Báo cáo hàng hóa", range.DateLabel, query.DisplayMode),
            Rows = response.Items.Select(x => new ProductsReportRowApiDto
            {
                Code = x.Code,
                Name = x.Name,
                GroupName = x.GroupName,
                SalePrice = x.SalePrice,
                Stock = x.Stock,
                RowStatus = x.RowStatus,
            }).ToList(),
        };
    }

    public async Task<byte[]> ExportProductsExcelAsync(EndOfDayReportQuery query, CancellationToken cancellationToken = default)
    {
        var data = await GetProductsDataAsync(query, cancellationToken);
        var rows = data.Rows.Select(r => (IReadOnlyList<object?>)new object?[]
        {
            r.Code,
            r.Name,
            r.GroupName ?? "",
            r.SalePrice,
            r.Stock,
            r.RowStatus ?? "",
        }).ToList();
        return GenericListReportExcelFiller.Fill(
            ReportTemplatePaths.Resolve("ProductsReport.xlsx"),
            data.Meta.Title,
            data.Meta,
            ["Mã hàng", "Tên hàng", "Nhóm", "Giá bán", "Tồn", "Trạng thái"],
            rows);
    }

    public async Task<ReportDataDto<CustomersReportRowApiDto>> GetCustomersDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var range = ReportQueryMapper.ResolveRange(query);
        var paged = await _customerProfiles.ListPagedAsync(ReportQueryMapper.ToCustomerProfileListQuery(query), cancellationToken);
        return new ReportDataDto<CustomersReportRowApiDto>
        {
            Meta = BuildMeta("Báo cáo khách hàng", range.DateLabel, query.DisplayMode),
            Rows = paged.Items.Select(x => new CustomersReportRowApiDto
            {
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                Phone1 = x.Phone1,
                CustomerGroup = x.CustomerGroup,
                CurrentDebt = x.CurrentDebt,
                TotalSalesNet = x.TotalSalesNet,
            }).ToList(),
        };
    }

    public async Task<byte[]> ExportCustomersExcelAsync(EndOfDayReportQuery query, CancellationToken cancellationToken = default)
    {
        var data = await GetCustomersDataAsync(query, cancellationToken);
        var rows = data.Rows.Select(r => (IReadOnlyList<object?>)new object?[]
        {
            r.CustomerCode,
            r.CustomerName,
            r.Phone1,
            r.CustomerGroup,
            r.CurrentDebt,
            r.TotalSalesNet,
        }).ToList();
        return GenericListReportExcelFiller.Fill(
            ReportTemplatePaths.Resolve("CustomersReport.xlsx"),
            data.Meta.Title,
            data.Meta,
            ["Mã KH", "Tên KH", "SĐT", "Nhóm", "Nợ hiện tại", "Doanh số thuần"],
            rows);
    }

    public async Task<ReportDataDto<SuppliersReportRowApiDto>> GetSuppliersDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var range = ReportQueryMapper.ResolveRange(query);
        var items = await _suppliers.ListAsync(ReportQueryMapper.ToSuppliersQuery(query), cancellationToken);
        return new ReportDataDto<SuppliersReportRowApiDto>
        {
            Meta = BuildMeta("Báo cáo nhà cung cấp", range.DateLabel, query.DisplayMode),
            Rows = items.Select(x => new SuppliersReportRowApiDto
            {
                Code = x.Code,
                Name = x.Name,
                SupplierGroupName = x.SupplierGroupName,
                CurrentDebt = x.CurrentDebt,
                TotalPurchase = x.TotalPurchase,
            }).ToList(),
        };
    }

    public async Task<byte[]> ExportSuppliersExcelAsync(EndOfDayReportQuery query, CancellationToken cancellationToken = default)
    {
        var data = await GetSuppliersDataAsync(query, cancellationToken);
        var rows = data.Rows.Select(r => (IReadOnlyList<object?>)new object?[]
        {
            r.Code,
            r.Name,
            r.SupplierGroupName ?? "",
            r.CurrentDebt,
            r.TotalPurchase,
        }).ToList();
        return GenericListReportExcelFiller.Fill(
            ReportTemplatePaths.Resolve("SuppliersReport.xlsx"),
            data.Meta.Title,
            data.Meta,
            ["Mã NCC", "Tên NCC", "Nhóm", "Nợ cần trả", "Tổng mua"],
            rows);
    }

    private async Task<byte[]> ExportEndOfDayExcelCoreAsync(
        EndOfDayReportQuery query,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range,
        CancellationToken cancellationToken)
    {
        var concern = query.Concern.Trim().ToLowerInvariant();

        return concern switch
        {
            "cashflow" => await ExportCashflowExcelAsync(query, range, cancellationToken),
            "products" => await ExportProductsExcelAsync(query, range, cancellationToken),
            "summary" => await ExportSummaryExcelAsync(query, range, cancellationToken),
            _ => await ExportSalesExcelAsync(query, range, cancellationToken),
        };
    }

    private async Task<byte[]> ExportSalesExcelAsync(
        EndOfDayReportQuery query,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range,
        CancellationToken cancellationToken)
    {
        var rows = await _invoices.ListForEndOfDayReportAsync(query, range.FromUtc, range.ToUtc, cancellationToken);
        var isHorizontal = IsHorizontal(query.DisplayMode);
        var templateName = isHorizontal ? "EndOfDayDocumentLC.xlsx" : "EndOfDayDocument.xlsx";
        var templatePath = ResolveTemplate(templateName);

        return isHorizontal
            ? EndOfDayExcelTemplateFiller.FillHorizontal(templatePath, query, rows, range)
            : EndOfDayExcelTemplateFiller.FillVertical(templatePath, query, rows, range);
    }

    private async Task<byte[]> ExportCashflowExcelAsync(
        EndOfDayReportQuery query,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range,
        CancellationToken cancellationToken)
    {
        if (IsHorizontal(query.DisplayMode))
        {
            var rows = await _cashbook.ListForEndOfDayReportAsync(
                query, range.FromUtc, range.ToUtc, cancellationToken);
            var templatePath = ResolveTemplate("EndOfDayCashFlowLC.xlsx");
            return EndOfDayCashFlowExcelTemplateFiller.FillHorizontal(templatePath, rows, range);
        }

        var agg = await _cashbook.AggregateForEndOfDayReportAsync(
            query, range.FromUtc, range.ToUtc, cancellationToken);
        var verticalPath = ResolveTemplate("EndOfDayCashFlow.xlsx");
        return EndOfDayCashFlowExcelTemplateFiller.FillVerticalCashflow(
            verticalPath, agg, range, "Báo cáo cuối ngày về thu chi");
    }

    private async Task<byte[]> ExportProductsExcelAsync(
        EndOfDayReportQuery query,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range,
        CancellationToken cancellationToken)
    {
        var rows = await _invoices.ListForEndOfDayProductsAsync(
            query, range.FromUtc, range.ToUtc, cancellationToken);
        var isHorizontal = IsHorizontal(query.DisplayMode);
        var templateName = isHorizontal ? "EndOfDayProductLC.xlsx" : "EndOfDayProduct.xlsx";
        var templatePath = ResolveTemplate(templateName);

        return isHorizontal
            ? EndOfDayProductExcelTemplateFiller.FillHorizontal(templatePath, rows, range)
            : EndOfDayProductExcelTemplateFiller.FillVertical(templatePath, rows, range);
    }

    private async Task<byte[]> ExportSummaryExcelAsync(
        EndOfDayReportQuery query,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range,
        CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryDtoAsync(query, range, cancellationToken);
        var templatePath = ResolveTemplate("EndOfDayCashFlow.xlsx");
        return EndOfDayCashFlowExcelTemplateFiller.FillSummary(templatePath, summary, range);
    }

    private async Task<EndOfDaySummaryDto> BuildSummaryDtoAsync(
        EndOfDayReportQuery query,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range,
        CancellationToken cancellationToken)
    {
        var summaryQuery = query with
        {
            CustomerSearch = null,
            PaymentMethod = null,
            SaleChannelId = null,
            ProductSearch = null,
            ProductType = null,
            ProductGroupId = null,
            GroupSameProducts = false,
        };

        var cashflow = await _cashbook.AggregateForEndOfDayReportAsync(
            summaryQuery, range.FromUtc, range.ToUtc, cancellationToken);
        var (valueRows, countRows) = await _invoices.GetSalesSummaryForEndOfDayAsync(
            summaryQuery, range.FromUtc, range.ToUtc, cancellationToken);

        return new EndOfDaySummaryDto
        {
            CashflowRows = cashflow,
            SalesValueRows = valueRows,
            SalesCountRows = countRows,
        };
    }

    private async Task<IReadOnlyList<EndOfDayUnifiedRowApiDto>> BuildSummaryGridRowsAsync(
        EndOfDayReportQuery query,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range,
        CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryDtoAsync(query, range, cancellationToken);
        var rows = new List<EndOfDayUnifiedRowApiDto>();

        foreach (var row in summary.CashflowRows)
            rows.Add(MapCashflowAggregateRow(row));

        foreach (var row in summary.SalesValueRows)
        {
            rows.Add(new EndOfDayUnifiedRowApiDto
            {
                Label = row.Label,
                Value = row.Value,
                CashAmount = row.CashAmount,
                TransferAmount = row.TransferAmount,
                CardAmount = row.CardAmount,
            });
        }

        foreach (var row in summary.SalesCountRows)
        {
            rows.Add(new EndOfDayUnifiedRowApiDto
            {
                Label = row.Label,
                TransactionCount = row.TransactionCount,
                CashAmount = row.CashAmount,
                TransferAmount = row.TransferAmount,
            });
        }

        return rows;
    }

    private static string ResolveTemplate(string templateName)
    {
        var templatePath = ReportTemplatePaths.Resolve(templateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Không tìm thấy template báo cáo: {templateName}", templatePath);
        return templatePath;
    }

    private static bool IsHorizontal(string displayMode) =>
        string.Equals(displayMode, "horizontal", StringComparison.OrdinalIgnoreCase);

    private static void ValidateEndOfDayConcern(EndOfDayReportQuery query)
    {
        var concern = query.Concern.Trim().ToLowerInvariant();
        if (concern is not ("sales" or "cashflow" or "products" or "summary"))
            throw new InvalidOperationException("Mối quan tâm báo cáo cuối ngày không hợp lệ.");
    }

    private static ReportMetaDto BuildMeta(string title, string dateLabel, string displayMode) =>
        new()
        {
            Title = title,
            DateLabel = dateLabel,
            BranchLabel = "Chi nhánh trung tâm",
            GeneratedAtLocal = DateTime.Now,
            DisplayMode = displayMode,
        };

    private static EndOfDayUnifiedRowApiDto MapSalesRow(EndOfDaySalesRowDto row) =>
        new()
        {
            Code = row.Code,
            OccurredAtUtc = row.CreatedAtUtc,
            CustomerName = row.CustomerName,
            SellerDisplayName = row.SellerDisplayName,
            Quantity = row.Quantity,
            SubtotalAmount = row.SubtotalAmount,
            DiscountAmount = row.DiscountAmount,
            PaidAmount = row.PaidAmount,
        };

    private static EndOfDayUnifiedRowApiDto MapCashflowDetailRow(EndOfDayCashflowRowDto row) =>
        new()
        {
            Code = row.Code,
            OccurredAtUtc = row.OccurredAtUtc,
            CategoryName = row.CategoryName,
            SellerDisplayName = row.StaffDisplayName,
            CounterpartyName = row.CounterpartyName,
            EntryType = row.EntryType == "Receipt" ? "Thu" : "Chi",
            Amount = row.Amount,
            SourceCode = row.SourceCode,
        };

    private static EndOfDayUnifiedRowApiDto MapCashflowAggregateRow(EndOfDayCashflowAggregateRowDto row) =>
        new()
        {
            Label = row.Label,
            CashAmount = row.CashAmount,
            TransferAmount = row.TransferAmount,
            CardAmount = row.CardAmount,
        };

    private static EndOfDayUnifiedRowApiDto MapProductRow(EndOfDayProductRowDto row) =>
        new()
        {
            ProductCode = row.ProductCode,
            ProductName = row.ProductName,
            Quantity = row.SoldQuantity,
            ReturnQuantity = row.ReturnQuantity,
            SubtotalAmount = row.Revenue,
            ReturnValue = row.ReturnValue,
            NetRevenue = row.NetRevenue,
            ListPrice = row.ListPrice,
            Variance = row.Variance,
        };

    private static DateTime ToLocal(DateTime utc) =>
        utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
}
