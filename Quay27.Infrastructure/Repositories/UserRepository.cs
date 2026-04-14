using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListWithRolesAsync(CancellationToken cancellationToken = default) =>
        await _db.Users.AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

    public Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId, CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking()
            .AnyAsync(u => u.Username == username && (!excludeUserId.HasValue || u.Id != excludeUserId.Value),
                cancellationToken);

    public void Add(User user) => _db.Users.Add(user);
    public void RemoveRange(IEnumerable<User> users) => _db.Users.RemoveRange(users);

    public async Task SetRolesByNamesAsync(Guid userId, IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default)
    {
        var distinctNames = roleNames.Distinct(StringComparer.Ordinal).ToList();
        var roles = await _db.Roles.AsNoTracking()
            .Where(r => distinctNames.Contains(r.Name))
            .ToListAsync(cancellationToken);

        if (roles.Count != distinctNames.Count)
            throw new InvalidOperationException("One or more role names are invalid.");

        var existing = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        _db.UserRoles.RemoveRange(existing);

        foreach (var role in roles)
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
    }

    public async Task<HashSet<string>> GetExistingRoleNamesSetAsync(CancellationToken cancellationToken = default) =>
        (await _db.Roles.AsNoTracking().Select(r => r.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
}
