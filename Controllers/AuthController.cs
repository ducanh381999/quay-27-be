using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Auth;

namespace Quay27_Be.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
