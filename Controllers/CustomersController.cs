using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Customers;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    public sealed class ImportCustomersExcelForm
    {
        public IFormFile? File { get; set; }
        public DateOnly SheetDate { get; set; }
    }

    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(
        [FromQuery] DateOnly? sheetDate,
        [FromQuery] int? queueId,
        CancellationToken cancellationToken)
    {
        var items = await _customerService.ListBySheetDateAsync(sheetDate, queueId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _customerService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();
        return Ok(item);
    }

    [HttpGet("{id:guid}/audit-logs")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerAuditLogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CustomerAuditLogEntryDto>>> GetAuditLogs(Guid id,
        CancellationToken cancellationToken)
    {
        var items = await _customerService.GetAuditLogsForCustomerAsync(id, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var created = await _customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("import-excel")]
    [ProducesResponseType(typeof(ImportCustomersExcelResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImportCustomersExcelResult>> ImportExcel(
        [FromForm] ImportCustomersExcelForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new { title = "Invalid file", detail = "Vui lòng chọn file Excel hợp lệ." });

        await using var ms = new MemoryStream();
        await form.File.CopyToAsync(ms, cancellationToken);
        var result = await _customerService.ImportExcelAsync(
            new ImportCustomersExcelRequest(ms.ToArray(), form.File.FileName, form.SheetDate),
            cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var updated = await _customerService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _customerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/queues/{queueId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetQueue(Guid id, int queueId, [FromBody] SetCustomerQueueRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest();
        await _customerService.SetQueueAsync(id, queueId, request, cancellationToken);
        return NoContent();
    }
}
