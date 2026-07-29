using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Purchasing;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/import-orders")]
public class ImportOrdersController : ControllerBase
{
    private readonly IImportOrderService _service;

    public ImportOrdersController(IImportOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ImportOrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ImportOrderListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? createdByUserIds,
        [FromQuery] string? receivedByUserIds,
        CancellationToken cancellationToken = default)
    {
        // FE currently sends stub user option values as CSV; treat first token as username filter when present.
        var createdBy = FirstCsvToken(createdByUserIds);
        var orderedBy = FirstCsvToken(receivedByUserIds);
        var items = await _service.ListAsync(
            new ImportOrderListQuery(
                search,
                OrderQuerySplit.SplitStrings(status),
                from,
                to,
                createdBy,
                orderedBy),
            cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ImportOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportOrderDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpGet("{id:guid}/supplier-payment-cashbook-entries")]
    [ProducesResponseType(typeof(IReadOnlyList<GoodsReceiptSupplierPaymentCashbookRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<GoodsReceiptSupplierPaymentCashbookRowDto>>> ListSupplierPaymentCashbookEntries(
        Guid id,
        CancellationToken cancellationToken)
    {
        var items = await _service.ListSupplierPaymentCashbookEntriesAsync(id, cancellationToken);
        if (items is null) return NotFound();
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ImportOrderDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ImportOrderDetailDto>> Create(
        [FromBody] CreateImportOrderRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ImportOrderDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportOrderDetailDto>> Update(
        Guid id,
        [FromBody] CreateImportOrderRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("suggest")]
    [ProducesResponseType(typeof(ImportOrderSuggestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportOrderSuggestResult>> Suggest(
        [FromBody] ImportOrderSuggestRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SuggestAsync(request, cancellationToken);
        return Ok(result);
    }

    private static string? FirstCsvToken(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;
        var part = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(part) ? null : part;
    }
}
