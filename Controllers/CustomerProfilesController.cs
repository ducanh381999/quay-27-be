using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Common;
using Quay27.Application.CustomerProfiles;

namespace Quay27_Be.Controllers;

public sealed class ImportCustomerProfilesExcelForm
{
    public IFormFile? File { get; set; }

    /// <summary>When true, skip rows whose Phone1 matches an existing active profile.</summary>
    public bool SkipDuplicatesByPhone { get; set; }

    /// <summary>When true, apply optional DuNoCuoi column as manual CRM debt on new profiles.</summary>
    public bool UpdateClosingDebt { get; set; }

    /// <summary>When false, reject import row if Email matches another active profile.</summary>
    public bool AllowDuplicateCustomerEmails { get; set; }
}

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CustomerProfilesController : ControllerBase
{
    private readonly ICustomerProfileService _service;

    public CustomerProfilesController(ICustomerProfileService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerProfileDto>>> List(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var items = await _service.ListAsync(search, cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Paginated CRM grid with invoice/return aggregates. The non-paged GET list remains for small consumers (e.g. order customer combobox).
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResult<CustomerProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerProfileListItemDto>>> ListPaged(
        [FromQuery] CustomerProfileListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _service.ListPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("creators")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> ListCreators(CancellationToken cancellationToken)
    {
        var items = await _service.ListDistinctCreatorsAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerProfileDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpGet("import/template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadImportTemplate()
    {
        var bytes = CustomerProfileImportTemplateBuilder.Build();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauFileKhachHang.xlsx");
    }

    [HttpPost("import-excel")]
    [ProducesResponseType(typeof(ImportCustomerProfilesExcelResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImportCustomerProfilesExcelResult>> ImportExcel(
        [FromForm] ImportCustomerProfilesExcelForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new { title = "Invalid file", detail = "Vui lòng chọn file Excel hợp lệ." });

        await using var ms = new MemoryStream();
        await form.File.CopyToAsync(ms, cancellationToken);
        var result = await _service.ImportExcelAsync(
            new ImportCustomerProfilesExcelRequest(
                ms.ToArray(),
                form.File.FileName,
                form.SkipDuplicatesByPhone,
                form.UpdateClosingDebt,
                form.AllowDuplicateCustomerEmails),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerProfileDto>> Create([FromBody] CreateCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerProfileDto>> Patch(Guid id, [FromBody] PatchCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _service.PatchAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok();
    }
}
