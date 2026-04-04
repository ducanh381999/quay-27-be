namespace Quay27.Application.Auth;

public sealed class CurrentUserResponse
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public bool CanDeleteCustomerRows { get; init; }
}
