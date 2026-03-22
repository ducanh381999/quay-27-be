using Quay27.Application.Auth;

namespace Quay27.Application.Abstractions;

public interface IJwtTokenIssuer
{
    TokenResponse CreateToken(Guid userId, string username, IReadOnlyList<string> roles);
}
