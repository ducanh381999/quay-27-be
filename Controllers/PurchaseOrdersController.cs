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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PatchStatus(Guid id, [FromBody] PatchOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _service.PatchStatusAsync(id, request, cancellationToken);
        return NoContent();
    }
}
