using Microsoft.AspNetCore.Identity;
using Quay27.Application.Abstractions;
using Quay27.Application.Auth;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenIssuer _jwtTokenIssuer;

    public AuthService(IUserRepository users, IPasswordHasher<User> passwordHasher, IJwtTokenIssuer jwtTokenIssuer)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenIssuer = jwtTokenIssuer;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            return null;

        var roles = await _users.GetRoleNamesAsync(user.Id, cancellationToken);
        return _jwtTokenIssuer.CreateToken(user.Id, user.Username, roles);
    }
}
