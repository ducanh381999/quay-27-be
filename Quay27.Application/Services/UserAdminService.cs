using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;
using Quay27.Application.Users;
using Quay27.Domain.Constants;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class UserAdminService : IUserAdminService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IColumnPermissionRepository _columnPermissions;
    private const int MaxDraftStaffNameLength = 200;
    private const int MaxDraftStaffNameCount = 200;

    private readonly ISheetPickerDraftStaffNameRepository _draftStaffNames;

    public UserAdminService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IColumnPermissionRepository columnPermissions,
        ISheetPickerDraftStaffNameRepository draftStaffNames)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _columnPermissions = columnPermissions;
        _draftStaffNames = draftStaffNames;
    }

    public async Task<IReadOnlyList<UserPickerDto>> ListForSheetPickersAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _users.ListWithRolesAsync(cancellationToken);
        var fromUsers = list
            .Where(u => u.IsActive && (UserHasAdminRole(u) || UserHasStaffRole(u)))
            .Select(u => new UserPickerDto { Username = u.Username, FullName = u.FullName })
            .ToList();

        var seen = new HashSet<string>(
            fromUsers.Select(u => u.FullName.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var custom = await _draftStaffNames.ListOrderedAsync(cancellationToken);
        foreach (var raw in custom)
        {
            var n = raw.Trim();
            if (n.Length == 0 || seen.Contains(n))
                continue;
            seen.Add(n);
            fromUsers.Add(new UserPickerDto { Username = string.Empty, FullName = n });
        }

        return fromUsers
            .OrderBy(u => u.FullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<IReadOnlyList<string>> ListSheetPickerDraftNamesAsync(CancellationToken cancellationToken = default) =>
        _draftStaffNames.ListOrderedAsync(cancellationToken);

    public async Task ReplaceSheetPickerDraftNamesAsync(IReadOnlyList<string> displayNames,
        CancellationToken cancellationToken = default)
    {
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in displayNames)
        {
            var n = raw.Trim();
            if (n.Length == 0 || seen.Contains(n))
                continue;
            if (n.Length > MaxDraftStaffNameLength)
                throw new ValidationException(new[]
                {
                    new ValidationFailure("names", $"Each name must be at most {MaxDraftStaffNameLength} characters.")
                });
            seen.Add(n);
            normalized.Add(n);
            if (normalized.Count > MaxDraftStaffNameCount)
                throw new ValidationException(new[]
                {
                    new ValidationFailure("names", $"At most {MaxDraftStaffNameCount} names are allowed.")
                });
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _draftStaffNames.ReplaceAllAsync(normalized, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _users.ListWithRolesAsync(cancellationToken);
        return list.Select(MapUser).ToList();
    }

    public async Task<UserSummaryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");
        return MapUser(user);
    }

    public async Task<UserSummaryDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var roleNames = NormalizeRoleNames(request.RoleNames);
        await ValidateRoleNamesAsync(roleNames, cancellationToken);

        var username = request.Username.Trim();
        if (await _users.UsernameExistsAsync(username, null, cancellationToken))
            throw new ConflictException("Username already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            FullName = request.FullName.Trim(),
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            PasswordHash = string.Empty
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _users.Add(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _users.SetRolesByNamesAsync(user.Id, roleNames, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (ShouldSeedDefaultCustomerPermissions(roleNames))
            {
                var defaults = SchemaConstants.GetAllCustomerColumnNames()
                    .Select(n => new ColumnPermissionRow(n, CanView: true, CanEdit: false))
                    .ToList();
                await _columnPermissions.ReplaceForUserAndTableAsync(user.Id, SchemaConstants.CustomersTable, defaults,
                    cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }, cancellationToken);

        var created = await _users.GetByIdAsync(user.Id, cancellationToken);
        return MapUser(created!);
    }

    public async Task<UserSummaryDto> PatchAsync(Guid id, PatchUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetTrackedByIdAsync(id, cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");

        if (request.Username is not null)
        {
            var newUsername = request.Username.Trim();
            if (await _users.UsernameExistsAsync(newUsername, id, cancellationToken))
                throw new ConflictException("Username already exists.");
            user.Username = newUsername;
        }

        if (request.FullName is not null)
            user.FullName = request.FullName.Trim();

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (request.CanDeleteCustomers.HasValue)
            user.CanDeleteCustomers = request.CanDeleteCustomers.Value;

        if (request.RoleNames is not null)
        {
            var roleNames = NormalizeRoleNames(request.RoleNames);
            await ValidateRoleNamesAsync(roleNames, cancellationToken);
            await _users.SetRolesByNamesAsync(id, roleNames, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _users.GetByIdAsync(id, cancellationToken);
        return MapUser(updated!);
    }

    public async Task ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetTrackedByIdAsync(id, cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static UserSummaryDto MapUser(User u)
    {
        var roles = u.UserRoles
            .Select(ur => ur.Role?.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n, StringComparer.Ordinal)
            .Cast<string>()
            .ToList();

        return new UserSummaryDto
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            IsActive = u.IsActive,
            CanDeleteCustomers = u.CanDeleteCustomers,
            Roles = roles
        };
    }

    private static bool UserHasAdminRole(User u) =>
        u.UserRoles.Any(ur =>
            string.Equals(ur.Role?.Name, SchemaConstants.Roles.Admin, StringComparison.Ordinal));

    private static bool UserHasStaffRole(User u) =>
        u.UserRoles.Any(ur =>
            string.Equals(ur.Role?.Name, SchemaConstants.Roles.Staff, StringComparison.Ordinal));

    private static List<string> NormalizeRoleNames(IReadOnlyList<string> roleNames) =>
        roleNames.Select(r => r.Trim()).Where(r => r.Length > 0).Distinct(StringComparer.Ordinal).ToList();

    private async Task ValidateRoleNamesAsync(IReadOnlyList<string> roleNames, CancellationToken cancellationToken)
    {
        if (roleNames.Count == 0)
            throw new ValidationException(new[]
            {
                new ValidationFailure("roleNames", "At least one role is required.")
            });

        var valid = await _users.GetExistingRoleNamesSetAsync(cancellationToken);
        foreach (var r in roleNames)
        {
            if (!valid.Contains(r))
                throw new ValidationException(new[]
                {
                    new ValidationFailure("roleNames", $"Unknown role: '{r}'.")
                });
        }
    }

    private static bool ShouldSeedDefaultCustomerPermissions(IReadOnlyList<string> roleNames)
    {
        var set = roleNames.ToHashSet(StringComparer.Ordinal);
        if (set.Contains(SchemaConstants.Roles.Admin))
            return false;
        return set.Contains(SchemaConstants.Roles.Staff);
    }
}
