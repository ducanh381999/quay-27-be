namespace Quay27.Application.Setup;

public record DemoSeedResponse(
    bool AlreadySeeded,
    string Message,
    IReadOnlyList<SeededUserInfo> Users,
    int CustomersCreated);

public record SeededUserInfo(string Username, string Password, string Role, string FullName);
