using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Setup;
using Quay27.Domain.Constants;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize(Roles = SchemaConstants.Roles.Admin)]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly IDemoDataSeedService _demoDataSeedService;

    public SetupController(IDemoDataSeedService demoDataSeedService)
    {
        _demoDataSeedService = demoDataSeedService;
    }

    /// <summary>Seed demo users (4 staff), column permissions, sample customers, queue rows, duplicate pair. Idempotent per DB (marker on customers).</summary>
    [HttpPost("demo-data")]
    [ProducesResponseType(typeof(DemoSeedResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DemoSeedResponse>> SeedDemoData(CancellationToken cancellationToken)
    {
        var result = await _demoDataSeedService.SeedDemoDataAsync(cancellationToken);
        return Ok(result);
    }
}
