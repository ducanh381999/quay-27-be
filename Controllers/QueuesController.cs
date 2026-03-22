using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Queues;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class QueuesController : ControllerBase
{
    private readonly IQueueService _queueService;

    public QueuesController(IQueueService queueService)
    {
        _queueService = queueService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<QueueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QueueDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _queueService.ListActiveAsync(cancellationToken);
        return Ok(items);
    }
}
