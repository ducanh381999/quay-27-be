using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Reports;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportsService _reports;

    public ReportsController(IReportsService reports) => _reports = reports;

    [HttpGet("end-of-day/data")]
    [ProducesResponseType(typeof(ReportDataDto<EndOfDayUnifiedRowApiDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<ReportDataDto<EndOfDayUnifiedRowApiDto>>> EndOfDayData(
        [FromQuery] ReportQueryParams p,
        CancellationToken cancellationToken) =>
        Execute(() => _reports.GetEndOfDayDataAsync(p.ToQuery(), cancellationToken));

    [HttpGet("end-of-day/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> EndOfDayExcel([FromQuery] ReportQueryParams p, CancellationToken cancellationToken) =>
        ExecuteFile(() => _reports.ExportEndOfDayExcelAsync(p.ToQuery(), cancellationToken), "BaoCaoCuoiNgay");

    [HttpGet("sales/data")]
    [ProducesResponseType(typeof(ReportDataDto<SalesReportRowApiDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<ReportDataDto<SalesReportRowApiDto>>> SalesData(
        [FromQuery] ReportQueryParams p,
        CancellationToken cancellationToken) =>
        Execute(() => _reports.GetSalesDataAsync(p.ToQuery(), cancellationToken));

    [HttpGet("sales/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> SalesExcel([FromQuery] ReportQueryParams p, CancellationToken cancellationToken) =>
        ExecuteFile(() => _reports.ExportSalesExcelAsync(p.ToQuery(), cancellationToken), "BaoCaoBanHang");

    [HttpGet("orders/data")]
    [ProducesResponseType(typeof(ReportDataDto<OrdersReportRowApiDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<ReportDataDto<OrdersReportRowApiDto>>> OrdersData(
        [FromQuery] ReportQueryParams p,
        CancellationToken cancellationToken) =>
        Execute(() => _reports.GetOrdersDataAsync(p.ToQuery(), cancellationToken));

    [HttpGet("orders/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> OrdersExcel([FromQuery] ReportQueryParams p, CancellationToken cancellationToken) =>
        ExecuteFile(() => _reports.ExportOrdersExcelAsync(p.ToQuery(), cancellationToken), "BaoCaoDatHang");

    [HttpGet("products/data")]
    [ProducesResponseType(typeof(ReportDataDto<ProductsReportRowApiDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<ReportDataDto<ProductsReportRowApiDto>>> ProductsData(
        [FromQuery] ReportQueryParams p,
        CancellationToken cancellationToken) =>
        Execute(() => _reports.GetProductsDataAsync(p.ToQuery(), cancellationToken));

    [HttpGet("products/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> ProductsExcel([FromQuery] ReportQueryParams p, CancellationToken cancellationToken) =>
        ExecuteFile(() => _reports.ExportProductsExcelAsync(p.ToQuery(), cancellationToken), "BaoCaoHangHoa");

    [HttpGet("customers/data")]
    [ProducesResponseType(typeof(ReportDataDto<CustomersReportRowApiDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<ReportDataDto<CustomersReportRowApiDto>>> CustomersData(
        [FromQuery] ReportQueryParams p,
        CancellationToken cancellationToken) =>
        Execute(() => _reports.GetCustomersDataAsync(p.ToQuery(), cancellationToken));

    [HttpGet("customers/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> CustomersExcel([FromQuery] ReportQueryParams p, CancellationToken cancellationToken) =>
        ExecuteFile(() => _reports.ExportCustomersExcelAsync(p.ToQuery(), cancellationToken), "BaoCaoKhachHang");

    [HttpGet("suppliers/data")]
    [ProducesResponseType(typeof(ReportDataDto<SuppliersReportRowApiDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<ReportDataDto<SuppliersReportRowApiDto>>> SuppliersData(
        [FromQuery] ReportQueryParams p,
        CancellationToken cancellationToken) =>
        Execute(() => _reports.GetSuppliersDataAsync(p.ToQuery(), cancellationToken));

    [HttpGet("suppliers/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> SuppliersExcel([FromQuery] ReportQueryParams p, CancellationToken cancellationToken) =>
        ExecuteFile(() => _reports.ExportSuppliersExcelAsync(p.ToQuery(), cancellationToken), "BaoCaoNhaCungCap");

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = "Report failed", detail = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new { title = "Template not found", detail = ex.Message });
        }
    }

    private async Task<IActionResult> ExecuteFile(Func<Task<byte[]>> action, string baseName)
    {
        try
        {
            var bytes = await action();
            var fileName = $"{baseName}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = "Export failed", detail = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new { title = "Template not found", detail = ex.Message });
        }
    }
}

public sealed class ReportQueryParams
{
    public string? DisplayMode { get; set; }
    public string? Concern { get; set; }
    public string? TimeMode { get; set; }
    public string? SingleDate { get; set; }
    public string? TimeFrom { get; set; }
    public string? TimeTo { get; set; }
    public string? CustomDateFrom { get; set; }
    public string? CustomDateTo { get; set; }
    public string? CustomerSearch { get; set; }
    public Guid? SellerUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? PaymentMethod { get; set; }
    public Guid? SaleChannelId { get; set; }
    public bool GroupSameProducts { get; set; }
    public string? ProductSearch { get; set; }
    public string? ProductType { get; set; }
    public Guid? ProductGroupId { get; set; }

    public EndOfDayReportQuery ToQuery() =>
        new(
            string.IsNullOrWhiteSpace(DisplayMode) ? "vertical" : DisplayMode.Trim(),
            string.IsNullOrWhiteSpace(Concern) ? "sales" : Concern.Trim(),
            string.IsNullOrWhiteSpace(TimeMode) ? "singleDay" : TimeMode.Trim(),
            SingleDate,
            TimeFrom,
            TimeTo,
            CustomDateFrom,
            CustomDateTo,
            CustomerSearch,
            SellerUserId,
            CreatedByUserId,
            PaymentMethod,
            SaleChannelId,
            GroupSameProducts,
            ProductSearch,
            ProductType,
            ProductGroupId);
}
