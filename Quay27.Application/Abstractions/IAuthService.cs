using Quay27.Application.Auth;

namespace Quay27.Application.Abstractions;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
