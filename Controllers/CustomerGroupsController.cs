using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.CustomerGroups;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CustomerGroupsController : ControllerBase
{
    private readonly ICustomerGroupService _service;

    public CustomerGroupsController(ICustomerGroupService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerGroupDto>>> List(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var items = await _service.ListAsync(search, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerGroupDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerGroupDto>> Create(
        [FromBody] CreateCustomerGroupRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerGroupDto>> Update(
        Guid id,
        [FromBody] UpdateCustomerGroupRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
