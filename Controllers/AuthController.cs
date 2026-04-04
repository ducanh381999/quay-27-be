using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Auth;
using Quay27.Application.Repositories;

namespace Quay27_Be.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;

    public AuthController(IAuthService authService, ICurrentUser currentUser, IUserRepository users)
    {
        _authService = authService;
        _currentUser = currentUser;
        _users = users;
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Unauthorized();

        var u = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (u is null || !u.IsActive)
            return Unauthorized();

        var isAdmin = _currentUser.IsAdmin;
        return Ok(new CurrentUserResponse
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            IsAdmin = isAdmin,
            Roles = _currentUser.Roles,
            CanDeleteCustomerRows = isAdmin || u.CanDeleteCustomers
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.LoginAsync(request, cancellationToken);
        if (token is null)
            return Unauthorized();
        return Ok(token);
    }
}
