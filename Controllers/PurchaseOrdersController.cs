using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Cashbook;
using Quay27.Application.Orders;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/purchase-orders")]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrdersController(IPurchaseOrderService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseOrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? statuses,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? deliveryPartner,
        [FromQuery] DateTime? deliveryFrom,
        [FromQuery] DateTime? deliveryTo,
        [FromQuery] string? provinces,
        [FromQuery] string? paymentMethods,
        [FromQuery] string? createdByUserIds,
        [FromQuery] string? receivedByUserIds,
        [FromQuery] string? saleChannelIds,
        CancellationToken cancellationToken)
    {
        var query = new PurchaseOrderListQuery(
            search,
            OrderQuerySplit.SplitStrings(statuses),
            from,
            to,
            deliveryPartner,
            deliveryFrom,
            deliveryTo,
            OrderQuerySplit.SplitStrings(provinces),
            OrderQuerySplit.SplitStrings(paymentMethods),
            OrderQuerySplit.SplitGuids(createdByUserIds),
            OrderQuerySplit.SplitGuids(receivedByUserIds),
            OrderQuerySplit.SplitGuids(saleChannelIds));
        var items = await _service.ListAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PurchaseOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(detail);
    }

    [HttpGet("{id:guid}/invoices")]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseOrderLinkedInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderLinkedInvoiceDto>>> ListInvoices(
        Guid id,
        CancellationToken cancellationToken)
    {
        var items = await _service.ListInvoicesAsync(id, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}/cashbook-entries")]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseOrderCashbookRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderCashbookRowDto>>> ListCashbookEntries(
        Guid id,
        CancellationToken cancellationToken)
    {
        var items = await _service.ListCashbookEntriesAsync(id, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderCreatedDto>> Create(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PatchStatus(Guid id, [FromBody] PatchOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _service.PatchStatusAsync(id, request, cancellationToken);
        return Ok();
    }
}
