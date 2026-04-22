using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.CustomerProfiles;

namespace Quay27_Be.Controllers;

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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerProfileDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken);
        return Ok(item);
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
