using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.CustomerProfiles;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/customer-profiles/{customerProfileId:guid}/delivery-addresses")]
public sealed class CustomerProfileDeliveryAddressesController : ControllerBase
{
    private readonly ICustomerDeliveryAddressService _service;

    public CustomerProfileDeliveryAddressesController(ICustomerDeliveryAddressService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerDeliveryAddressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerDeliveryAddressDto>>> List(Guid customerProfileId,
        CancellationToken cancellationToken) =>
        Ok(await _service.ListAsync(customerProfileId, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(CustomerDeliveryAddressDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDeliveryAddressDto>> Create(Guid customerProfileId,
        [FromBody] CreateCustomerDeliveryAddressRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(customerProfileId, request, cancellationToken);
        return CreatedAtAction(nameof(List), new { customerProfileId }, created);
    }

    [HttpPatch("{addressId:guid}")]
    [ProducesResponseType(typeof(CustomerDeliveryAddressDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDeliveryAddressDto>> Patch(Guid customerProfileId, Guid addressId,
        [FromBody] PatchCustomerDeliveryAddressRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.PatchAsync(customerProfileId, addressId, request, cancellationToken));

    [HttpDelete("{addressId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid customerProfileId, Guid addressId,
        CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(customerProfileId, addressId, cancellationToken);
        return NoContent();
    }
}
