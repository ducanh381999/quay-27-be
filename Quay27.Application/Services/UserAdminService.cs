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
    private readonly ISheetPickerMemberRepository _sheetPickerMembers;

    public UserAdminService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IColumnPermissionRepository columnPermissions,
        ISheetPickerMemberRepository sheetPickerMembers)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _columnPermissions = columnPermissions;
        _sheetPickerMembers = sheetPickerMembers;
    }

    public async Task<IReadOnlyList<UserPickerDto>> ListForSheetPickersAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _users.ListWithRolesAsync(cancellationToken);
        var memberIds = (await _sheetPickerMembers.ListUserIdsAsync(cancellationToken)).ToHashSet();

        return list
            .Where(u => u.IsActive && (UserHasAdminRole(u) || memberIds.Contains(u.Id)))
            .OrderBy(u => u.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(u => new UserPickerDto { Username = u.Username, FullName = u.FullName })
            .ToList();
    }

    public Task<IReadOnlyList<Guid>> ListSheetPickerMemberIdsAsync(CancellationToken cancellationToken = default) =>
        _sheetPickerMembers.ListUserIdsAsync(cancellationToken);

    public async Task ReplaceSheetPickerMembersAsync(IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var distinct = userIds.Distinct().ToList();
        foreach (var uid in distinct)
        {
            var u = await _users.GetByIdAsync(uid, cancellationToken);
            if (u is null)
                throw new NotFoundException("One or more users were not found.");
            if (!u.IsActive)
                throw new ValidationException(new[]
                {
                    new ValidationFailure("userIds", $"User '{u.Username}' is inactive.")
                });
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _sheetPickerMembers.ClearAsync(cancellationToken);
            foreach (var uid in distinct)
                _sheetPickerMembers.Add(uid);
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
