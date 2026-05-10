using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Orders;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/sales-channels")]
public sealed class SaleChannelsController : ControllerBase
{
    private readonly ISaleChannelService _channels;

    public SaleChannelsController(ISaleChannelService channels) => _channels = channels;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SaleChannelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SaleChannelDto>>> List(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var items = await _channels.ListAsync(includeInactive, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SaleChannelDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SaleChannelDto>> Create(
        [FromBody] CreateSaleChannelRequest body,
        CancellationToken cancellationToken)
    {
        var created = await _channels.CreateAsync(body, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }
}
