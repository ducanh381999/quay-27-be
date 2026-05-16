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
    private readonly IEndOfDayReportService _endOfDayReports;

    public ReportsController(IEndOfDayReportService endOfDayReports) => _endOfDayReports = endOfDayReports;

    [HttpGet("end-of-day/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EndOfDayPdf(
        [FromQuery] string? displayMode,
        [FromQuery] string? concern,
        [FromQuery] string? timeMode,
        [FromQuery] string? singleDate,
        [FromQuery] string? timeFrom,
        [FromQuery] string? timeTo,
        [FromQuery] string? customDateFrom,
        [FromQuery] string? customDateTo,
        [FromQuery] string? customerSearch,
        [FromQuery] Guid? sellerUserId,
        [FromQuery] Guid? createdByUserId,
        [FromQuery] string? paymentMethod,
        [FromQuery] Guid? saleChannelId,
        CancellationToken cancellationToken)
    {
        var query = new EndOfDayReportQuery(
            string.IsNullOrWhiteSpace(displayMode) ? "vertical" : displayMode.Trim(),
            string.IsNullOrWhiteSpace(concern) ? "sales" : concern.Trim(),
            string.IsNullOrWhiteSpace(timeMode) ? "singleDay" : timeMode.Trim(),
            singleDate,
            timeFrom,
            timeTo,
            customDateFrom,
            customDateTo,
            customerSearch,
            sellerUserId,
            createdByUserId,
            paymentMethod,
            saleChannelId);

        try
        {
            var pdf = await _endOfDayReports.GenerateSalesPdfAsync(query, cancellationToken);
            var suffix = string.Equals(query.DisplayMode, "horizontal", StringComparison.OrdinalIgnoreCase)
                ? "ngang"
                : "doc";
            var fileName = $"BaoCaoCuoiNgay_{suffix}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = "Report generation failed", detail = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new { title = "Template not found", detail = ex.Message });
        }
    }
}
