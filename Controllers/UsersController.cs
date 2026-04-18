using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Users;
using Quay27.Domain.Constants;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _userAdmin;
    private readonly ICustomerColumnPermissionService _columnPermissions;

    public UsersController(IUserAdminService userAdmin, ICustomerColumnPermissionService columnPermissions)
    {
        _userAdmin = userAdmin;
        _columnPermissions = columnPermissions;
    }

    [HttpGet("sheet-pickers")]
    [ProducesResponseType(typeof(IReadOnlyList<UserPickerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserPickerDto>>> SheetPickers(CancellationToken cancellationToken)
    {
        var items = await _userAdmin.ListForSheetPickersAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("sheet-picker-members")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> SheetPickerMembers(CancellationToken cancellationToken)
    {
        var names = await _userAdmin.ListSheetPickerDraftNamesAsync(cancellationToken);
        return Ok(names);
    }

    [HttpPut("sheet-picker-members")]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PutSheetPickerMembers([FromBody] SheetPickerMembersPutRequest? body,
        CancellationToken cancellationToken)
    {
        await _userAdmin.ReplaceSheetPickerDraftNamesAsync(body?.Names ?? Array.Empty<string>(), cancellationToken);
        return Ok();
    }

    [HttpGet]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _userAdmin.ListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _userAdmin.GetAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserSummaryDto>> Create([FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _userAdmin.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryDto>> Patch(Guid id, [FromBody] PatchUserRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _userAdmin.PatchAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("bulk-delete")]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkDelete([FromBody] IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        await _userAdmin.BulkDeleteAsync(ids, cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/password")]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userAdmin.ResetPasswordAsync(id, request, cancellationToken);
        return Ok();
    }

    [HttpGet("{id:guid}/column-permissions")]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(typeof(CustomerColumnPermissionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerColumnPermissionsResponse>> GetColumnPermissions(Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _columnPermissions.GetForUserAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/column-permissions")]
    [Authorize(Roles = SchemaConstants.Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutColumnPermissions(Guid id,
        [FromBody] IReadOnlyList<CustomerColumnPermissionInput> body, CancellationToken cancellationToken)
    {
        await _columnPermissions.ReplaceForUserAsync(id, body, cancellationToken);
        return Ok();
    }
}
