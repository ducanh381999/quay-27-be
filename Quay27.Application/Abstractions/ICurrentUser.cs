namespace Quay27.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string Username { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAdmin { get; }
}
