using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Settings;

namespace Quay27_Be.Controllers;

/// <summary>Read-only treasury catalogs + account list/create for POS (any authenticated user).</summary>
[ApiController]
[Authorize]
[Route("api/treasury-pos")]
public sealed class TreasuryPosController : ControllerBase
{
    private readonly ITreasurySettingsService _treasury;

    public TreasuryPosController(ITreasurySettingsService treasury) => _treasury = treasury;

    [HttpGet("bank-catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<BankCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BankCatalogItemDto>>> BankCatalog(
        CancellationToken cancellationToken) =>
        Ok(await _treasury.ListBankCatalogAsync(cancellationToken));

    [HttpGet("ewallet-catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<EWalletCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EWalletCatalogItemDto>>> EWalletCatalog(
        CancellationToken cancellationToken) =>
        Ok(await _treasury.ListEWalletCatalogAsync(cancellationToken));

    [HttpGet("accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<TreasuryAccountListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TreasuryAccountListItemDto>>> ListAccounts(
        [FromQuery] string kind,
        CancellationToken cancellationToken) =>
        Ok(await _treasury.ListAccountsAsync(kind, cancellationToken));

    [HttpGet("accounts/{id:guid}")]
    [ProducesResponseType(typeof(TreasuryAccountDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreasuryAccountDetailDto>> GetAccount(Guid id,
        CancellationToken cancellationToken)
    {
        var item = await _treasury.GetAccountAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("accounts")]
    [ProducesResponseType(typeof(TreasuryAccountDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TreasuryAccountDetailDto>> CreateAccount(
        [FromBody] CreateTreasuryAccountRequest body,
        CancellationToken cancellationToken)
    {
        var created = await _treasury.CreateAsync(body, cancellationToken);
        return CreatedAtAction(nameof(GetAccount), new { id = created.Id }, created);
    }
}
