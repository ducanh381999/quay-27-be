using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Suppliers;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/supplier-groups")]
public class SupplierGroupsController : ControllerBase
{
    private readonly ISupplierGroupService _service;

    public SupplierGroupsController(ISupplierGroupService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierGroupDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _service.ListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SupplierGroupDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SupplierGroupDto>> Create([FromBody] CreateSupplierGroupRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(SupplierGroupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierGroupDto>> Update(Guid id, [FromBody] UpdateSupplierGroupRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
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
