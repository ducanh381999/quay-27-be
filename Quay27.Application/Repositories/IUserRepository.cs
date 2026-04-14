using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListWithRolesAsync(CancellationToken cancellationToken = default);
    Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId, CancellationToken cancellationToken = default);
    void Add(User user);
    void RemoveRange(IEnumerable<User> users);
    Task SetRolesByNamesAsync(Guid userId, IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetExistingRoleNamesSetAsync(CancellationToken cancellationToken = default);
}
