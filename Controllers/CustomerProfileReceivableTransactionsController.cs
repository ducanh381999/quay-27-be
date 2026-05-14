using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.CustomerProfiles;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/customer-profiles/{customerProfileId:guid}/receivable-transactions")]
public sealed class CustomerProfileReceivableTransactionsController : ControllerBase
{
    private readonly ICustomerReceivableService _service;

    public CustomerProfileReceivableTransactionsController(ICustomerReceivableService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedReceivableTransactionsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedReceivableTransactionsResult>> List(
        Guid customerProfileId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? kind = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListTransactionsAsync(customerProfileId, skip, take, kind, cancellationToken));

    [HttpPost("payment")]
    [ProducesResponseType(typeof(CustomerReceivableTransactionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerReceivableTransactionDto>> Payment(Guid customerProfileId,
        [FromBody] RecordCustomerPaymentRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.RecordPaymentAsync(customerProfileId, request, cancellationToken));

    [HttpPost("adjustment")]
    [ProducesResponseType(typeof(CustomerReceivableTransactionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerReceivableTransactionDto>> Adjustment(Guid customerProfileId,
        [FromBody] RecordCustomerAdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.RecordAdjustmentAsync(customerProfileId, request, cancellationToken));

    [HttpPost("payment-discount")]
    [ProducesResponseType(typeof(CustomerReceivableTransactionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerReceivableTransactionDto>> PaymentDiscount(Guid customerProfileId,
        [FromBody] RecordCustomerPaymentDiscountRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.RecordPaymentDiscountAsync(customerProfileId, request, cancellationToken));

    [HttpPost("qr-confirm")]
    [ProducesResponseType(typeof(CustomerReceivableTransactionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerReceivableTransactionDto>> QrConfirm(Guid customerProfileId,
        [FromBody] RecordCustomerQrPaymentRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.RecordQrPaymentAsync(customerProfileId, request, cancellationToken));
}
