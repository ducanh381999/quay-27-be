using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Quay27.Application.Abstractions;
using Quay27.Application.Auth;
using Quay27_Be.Options;

namespace Quay27_Be.Security;

public class JwtTokenIssuer : IJwtTokenIssuer
{
    private readonly JwtOptions _options;

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TokenResponse CreateToken(Guid userId, string username, IReadOnlyList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResponse(jwt, expires);
    }
}
