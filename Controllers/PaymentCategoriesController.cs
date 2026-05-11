using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Cashbook;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/payment-categories")]
public sealed class PaymentCategoriesController : ControllerBase
{
    private readonly IPaymentCategoryService _service;

    public PaymentCategoriesController(IPaymentCategoryService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PaymentCategoryDto>>> List([FromQuery] string? kind,
        CancellationToken cancellationToken)
    {
        var items = await _service.ListAsync(kind, cancellationToken);
        return Ok(items);
    }
}
