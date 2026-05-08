using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Purchasing;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/receiving-accounts")]
public class ReceivingAccountsController : ControllerBase
{
    private readonly IReceivingAccountService _service;

    public ReceivingAccountsController(IReceivingAccountService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReceivingAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReceivingAccountDto>>> List(CancellationToken cancellationToken = default)
    {
        return Ok(await _service.ListAsync(cancellationToken));
    }
}
