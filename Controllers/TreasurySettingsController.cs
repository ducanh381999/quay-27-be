using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Settings;
using Quay27.Domain.Constants;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize(Roles = SchemaConstants.Roles.Admin)]
[Route("api/settings")]
public sealed class TreasurySettingsController : ControllerBase
{
    private readonly ITreasurySettingsService _treasury;

    public TreasurySettingsController(ITreasurySettingsService treasury) => _treasury = treasury;

    [HttpGet("bank-catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<BankCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BankCatalogItemDto>>> BankCatalog(CancellationToken cancellationToken)
        => Ok(await _treasury.ListBankCatalogAsync(cancellationToken));

    [HttpGet("ewallet-catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<EWalletCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EWalletCatalogItemDto>>> EWalletCatalog(
        CancellationToken cancellationToken)
        => Ok(await _treasury.ListEWalletCatalogAsync(cancellationToken));

    [HttpGet("treasury-accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<TreasuryAccountListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TreasuryAccountListItemDto>>> ListAccounts(
        [FromQuery] string kind,
        CancellationToken cancellationToken)
        => Ok(await _treasury.ListAccountsAsync(kind, cancellationToken));

    [HttpGet("treasury-accounts/{id:guid}")]
    [ProducesResponseType(typeof(TreasuryAccountDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreasuryAccountDetailDto>> GetAccount(Guid id, CancellationToken cancellationToken)
    {
        var item = await _treasury.GetAccountAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("treasury-accounts")]
    [ProducesResponseType(typeof(TreasuryAccountDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TreasuryAccountDetailDto>> Create(
        [FromBody] CreateTreasuryAccountRequest body,
        CancellationToken cancellationToken)
    {
        var created = await _treasury.CreateAsync(body, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPatch("treasury-accounts/{id:guid}")]
    [ProducesResponseType(typeof(TreasuryAccountDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TreasuryAccountDetailDto>> Patch(
        Guid id,
        [FromBody] PatchTreasuryAccountRequest body,
        CancellationToken cancellationToken)
        => Ok(await _treasury.PatchAsync(id, body, cancellationToken));

    [HttpDelete("treasury-accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _treasury.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
