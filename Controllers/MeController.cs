using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Users;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly ICustomerColumnPermissionService _columnPermissions;

    public MeController(ICustomerColumnPermissionService columnPermissions)
    {
        _columnPermissions = columnPermissions;
    }

    [HttpGet("customer-column-permissions")]
    [ProducesResponseType(typeof(CustomerColumnPermissionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerColumnPermissionsResponse>> GetCustomerColumnPermissions(
        CancellationToken cancellationToken)
    {
        var result = await _columnPermissions.GetForCurrentUserAsync(cancellationToken);
        return Ok(result);
    }
}
