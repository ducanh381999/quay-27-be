namespace Quay27.Application.Auth;

public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");
