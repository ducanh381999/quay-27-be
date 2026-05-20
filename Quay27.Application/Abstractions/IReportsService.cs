using Quay27.Application.Reports;

namespace Quay27.Application.Abstractions;

public interface IReportsService
{
    Task<ReportDataDto<EndOfDayUnifiedRowApiDto>> GetEndOfDayDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportEndOfDayExcelAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportDataDto<SalesReportRowApiDto>> GetSalesDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportSalesExcelAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportDataDto<OrdersReportRowApiDto>> GetOrdersDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportOrdersExcelAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportDataDto<ProductsReportRowApiDto>> GetProductsDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportProductsExcelAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportDataDto<CustomersReportRowApiDto>> GetCustomersDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCustomersExcelAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportDataDto<SuppliersReportRowApiDto>> GetSuppliersDataAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportSuppliersExcelAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default);
}
