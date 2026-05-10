using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Orders;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/returns")]
public sealed class ReturnsController : ControllerBase
{
    private readonly ISalesReturnService _service;

    public ReturnsController(ISalesReturnService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalesReturnListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalesReturnListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? returnTypes,
        [FromQuery] string? statuses,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? createdByUserIds,
        [FromQuery] string? receivedByUserIds,
        [FromQuery] string? saleChannelIds,
        [FromQuery] string? otherCollectionTypes,
        CancellationToken cancellationToken)
    {
        var query = new SalesReturnListQuery(
            search,
            OrderQuerySplit.SplitStrings(returnTypes),
            OrderQuerySplit.SplitStrings(statuses),
            from,
            to,
            OrderQuerySplit.SplitGuids(createdByUserIds),
            OrderQuerySplit.SplitGuids(receivedByUserIds),
            OrderQuerySplit.SplitGuids(saleChannelIds),
            OrderQuerySplit.SplitStrings(otherCollectionTypes));
        var items = await _service.ListAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderCreatedDto>> Create(
        [FromBody] CreateSalesReturnRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }
}
