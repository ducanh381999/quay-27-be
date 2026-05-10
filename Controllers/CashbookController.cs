using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Cashbook;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/cashbook")]
public sealed class CashbookController : ControllerBase
{
    private readonly ICashbookService _service;

    public CashbookController(ICashbookService service) => _service = service;

    [HttpGet("entries")]
    [ProducesResponseType(typeof(IReadOnlyList<CashbookEntryListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CashbookEntryListItemDto>>> ListEntries(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? fund,
        [FromQuery] string? entryTypes,
        [FromQuery] int? categoryId,
        [FromQuery] string? statuses,
        [FromQuery] string? affectsBusinessResult,
        [FromQuery] Guid? creatorUserId,
        [FromQuery] Guid? staffUserId,
        [FromQuery] string? search,
        [FromQuery] string? counterparty,
        [FromQuery] string? partyPhone,
        [FromQuery] string? debtModes,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var query = new CashbookListQuery(
            from,
            to,
            fund,
            OrderQuerySplit.SplitStrings(entryTypes),
            categoryId,
            OrderQuerySplit.SplitStrings(statuses),
            ParseBoolOpt(affectsBusinessResult),
            creatorUserId,
            staffUserId,
            search,
            counterparty,
            partyPhone,
            OrderQuerySplit.SplitStrings(debtModes),
            page,
            pageSize);
        var items = await _service.ListEntriesAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(CashbookSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CashbookSummaryDto>> Summary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? fund,
        [FromQuery] string? entryTypes,
        [FromQuery] int? categoryId,
        [FromQuery] string? statuses,
        [FromQuery] string? affectsBusinessResult,
        [FromQuery] Guid? creatorUserId,
        [FromQuery] Guid? staffUserId,
        [FromQuery] string? search,
        [FromQuery] string? counterparty,
        [FromQuery] string? partyPhone,
        [FromQuery] string? debtModes,
        CancellationToken cancellationToken = default)
    {
        var query = new CashbookListQuery(
            from,
            to,
            fund,
            OrderQuerySplit.SplitStrings(entryTypes),
            categoryId,
            OrderQuerySplit.SplitStrings(statuses),
            ParseBoolOpt(affectsBusinessResult),
            creatorUserId,
            staffUserId,
            search,
            counterparty,
            partyPhone,
            OrderQuerySplit.SplitStrings(debtModes),
            1,
            1);
        var summary = await _service.GetSummaryAsync(query, cancellationToken);
        return Ok(summary);
    }

    [HttpPost("parties")]
    [ProducesResponseType(typeof(CashbookPartyCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CashbookPartyCreatedDto>> CreateParty([FromBody] CreateCashbookPartyRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreatePartyAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("receipts")]
    [ProducesResponseType(typeof(CashbookEntryCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CashbookEntryCreatedDto>> CreateReceipt(
        [FromBody] CreateCashbookReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateReceiptAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("payments")]
    [ProducesResponseType(typeof(CashbookEntryCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CashbookEntryCreatedDto>> CreatePayment(
        [FromBody] CreateCashbookPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreatePaymentAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    private static bool? ParseBoolOpt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        if (bool.TryParse(s.Trim(), out var b))
            return b;
        return null;
    }
}
