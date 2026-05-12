using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Purchasing;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/return-receipts")]
public class ReturnReceiptsController : ControllerBase
{
    public sealed class ImportForm
    {
        public IFormFile? File { get; set; }
    }

    private readonly IReturnReceiptService _service;
    private readonly IWebHostEnvironment _environment;

    public ReturnReceiptsController(IReturnReceiptService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReturnReceiptListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReturnReceiptListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var items = await _service.ListAsync(new ReceiptListQuery(search, status, from, to), cancellationToken);
        return Ok(items);
    }

    [HttpGet("template-excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadTemplateExcel(CancellationToken cancellationToken)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "MauFileTraHangNhap.xlsx");
        if (!System.IO.File.Exists(templatePath))
            return NotFound(new { title = "Template not found", detail = "Không tìm thấy file template Excel trên server." });

        var fileName = Path.GetFileName(templatePath);
        var bytes = await System.IO.File.ReadAllBytesAsync(templatePath, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReturnReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnReceiptDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReturnReceiptDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReturnReceiptDto>> Create([FromBody] CreateReturnReceiptRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReturnReceiptDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReturnReceiptDto>> Update(Guid id, [FromBody] CreateReturnReceiptRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("import-preview")]
    [ProducesResponseType(typeof(ReceiptImportPreviewResult), StatusCodes.Status200OK)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ReceiptImportPreviewResult>> ImportPreview(
        [FromForm] ImportForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new { title = "Invalid file", detail = "Vui lòng chọn file Excel hợp lệ." });
        await using var ms = new MemoryStream();
        await form.File.CopyToAsync(ms, cancellationToken);
        var result = await _service.PreviewImportAsync(ms.ToArray(), form.File.FileName, cancellationToken);
        return Ok(result);
    }
}
