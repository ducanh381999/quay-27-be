using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Suppliers;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    public sealed class ImportSuppliersForm
    {
        public IFormFile? File { get; set; }
        public bool UpdateClosingDebt { get; set; } = true;
    }

    public sealed class SetActiveRequest
    {
        public bool Active { get; set; }
    }

    private readonly ISupplierService _suppliers;
    private readonly IWebHostEnvironment _environment;

    public SuppliersController(ISupplierService suppliers, IWebHostEnvironment environment)
    {
        _suppliers = suppliers;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? groupId,
        [FromQuery] string? status,
        [FromQuery] decimal? totalPurchaseMin,
        [FromQuery] decimal? totalPurchaseMax,
        [FromQuery] decimal? currentDebtMin,
        [FromQuery] decimal? currentDebtMax,
        [FromQuery] DateOnly? createdFrom,
        [FromQuery] DateOnly? createdTo,
        CancellationToken cancellationToken = default)
    {
        var query = new ListSuppliersQuery(
            search, groupId, status,
            totalPurchaseMin, totalPurchaseMax,
            currentDebtMin, currentDebtMax,
            createdFrom, createdTo);
        var items = await _suppliers.ListAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _suppliers.GetByIdAsync(id, cancellationToken);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var created = await _suppliers.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var updated = await _suppliers.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPut("{id:guid}/active")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDto>> SetActive(Guid id, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var updated = await _suppliers.SetActiveAsync(id, request.Active, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _suppliers.DeleteAsync(id, cancellationToken);
        return Ok();
    }

    [HttpPost("import-excel")]
    [ProducesResponseType(typeof(ImportSuppliersExcelResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImportSuppliersExcelResult>> ImportExcel(
        [FromForm] ImportSuppliersForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new { title = "Invalid file", detail = "Vui lòng chọn file Excel hợp lệ." });

        await using var ms = new MemoryStream();
        await form.File.CopyToAsync(ms, cancellationToken);
        var result = await _suppliers.ImportExcelAsync(
            new ImportSuppliersExcelRequest(ms.ToArray(), form.File.FileName, form.UpdateClosingDebt),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] string? search,
        [FromQuery] Guid? groupId,
        [FromQuery] string? status,
        [FromQuery] decimal? totalPurchaseMin,
        [FromQuery] decimal? totalPurchaseMax,
        [FromQuery] decimal? currentDebtMin,
        [FromQuery] decimal? currentDebtMax,
        [FromQuery] DateOnly? createdFrom,
        [FromQuery] DateOnly? createdTo,
        CancellationToken cancellationToken = default)
    {
        var query = new ListSuppliersQuery(
            search, groupId, status,
            totalPurchaseMin, totalPurchaseMax,
            currentDebtMin, currentDebtMax,
            createdFrom, createdTo);
        var bytes = await _suppliers.ExportExcelAsync(query, cancellationToken);
        var fileName = $"DanhSachNhaCungCap_KV{DateTime.Now:ddMMyyyy-HHmmss-fff}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("template-excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadTemplateExcel(CancellationToken cancellationToken)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "MauFileNhaCungCap.xlsx");
        if (!System.IO.File.Exists(templatePath))
            return NotFound(new { title = "Template not found", detail = "Không tìm thấy file template Excel trên server." });

        var fileName = Path.GetFileName(templatePath);
        var bytes = await System.IO.File.ReadAllBytesAsync(templatePath, cancellationToken);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
