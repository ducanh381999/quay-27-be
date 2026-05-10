using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Orders;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly ISalesInvoiceService _service;

    public InvoicesController(ISalesInvoiceService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalesInvoiceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalesInvoiceListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? invoiceDeliveryTypes,
        [FromQuery] string? statuses,
        [FromQuery] string? deliveryStatuses,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? deliveryPartner,
        [FromQuery] DateTime? deliveryFrom,
        [FromQuery] DateTime? deliveryTo,
        [FromQuery] string? provinces,
        [FromQuery] string? paymentMethods,
        [FromQuery] string? createdByUserIds,
        [FromQuery] string? sellerUserIds,
        [FromQuery] string? priceListIds,
        [FromQuery] string? saleChannelIds,
        CancellationToken cancellationToken)
    {
        var query = new SalesInvoiceListQuery(
            search,
            OrderQuerySplit.SplitStrings(invoiceDeliveryTypes),
            OrderQuerySplit.SplitStrings(statuses),
            OrderQuerySplit.SplitStrings(deliveryStatuses),
            from,
            to,
            deliveryPartner,
            deliveryFrom,
            deliveryTo,
            OrderQuerySplit.SplitStrings(provinces),
            OrderQuerySplit.SplitStrings(paymentMethods),
            OrderQuerySplit.SplitGuids(createdByUserIds),
            OrderQuerySplit.SplitGuids(sellerUserIds),
            OrderQuerySplit.SplitGuids(priceListIds),
            OrderQuerySplit.SplitGuids(saleChannelIds));
        var items = await _service.ListAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderCreatedDto>> Create(
        [FromBody] CreateSalesInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }
}
