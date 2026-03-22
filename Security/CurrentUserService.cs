using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Quay27.Application.Abstractions;
using Quay27.Domain.Constants;

namespace Quay27_Be.Security;

public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string Username => User?.Identity?.Name ?? User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsAdmin => Roles.Contains(SchemaConstants.Roles.Admin);
}
