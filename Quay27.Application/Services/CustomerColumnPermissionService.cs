using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;
using Quay27.Application.Users;
using Quay27.Domain.Constants;

namespace Quay27.Application.Services;

public class CustomerColumnPermissionService : ICustomerColumnPermissionService
{
    private readonly IColumnPermissionRepository _columnPermissions;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerColumnPermissionService(
        IColumnPermissionRepository columnPermissions,
        IUserRepository users,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _columnPermissions = columnPermissions;
        _users = users;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public Task<CustomerColumnPermissionsResponse> GetForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (_currentUser.IsAdmin)
            return Task.FromResult(FullAllowResponse());

        return BuildStaffResponseAsync(_currentUser.UserId!.Value, cancellationToken);
    }

    public async Task<CustomerColumnPermissionsResponse> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");

        var roles = await _users.GetRoleNamesAsync(userId, cancellationToken);
        if (roles.Contains(SchemaConstants.Roles.Admin))
            return FullAllowResponse();

        return await BuildStaffResponseAsync(userId, cancellationToken);
    }

    public async Task ReplaceForUserAsync(Guid userId, IReadOnlyList<CustomerColumnPermissionInput> items,
        CancellationToken cancellationToken = default)
    {
        if (await _users.GetByIdAsync(userId, cancellationToken) is null)
            throw new NotFoundException("User not found.");

        var allow = SchemaConstants.GetAllCustomerColumnNames().ToHashSet(StringComparer.Ordinal);
        ValidateReplaceBody(items, allow);

        var rows = items.Select(i => new ColumnPermissionRow(i.ColumnName.Trim(), i.CanView, i.CanEdit)).ToList();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _columnPermissions.ReplaceForUserAndTableAsync(userId, SchemaConstants.CustomersTable, rows,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    private static void ValidateReplaceBody(IReadOnlyList<CustomerColumnPermissionInput> items, HashSet<string> allow)
    {
        var failures = new List<ValidationFailure>();
        if (items.Count != allow.Count)
            failures.Add(new ValidationFailure("", $"Body must include exactly {allow.Count} column permission entries."));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var name = item.ColumnName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                failures.Add(new ValidationFailure("columnName", "Column name is required."));
                continue;
            }

            if (!allow.Contains(name))
                failures.Add(new ValidationFailure("columnName", $"Unknown column: '{name}'."));
            else if (!seen.Add(name))
                failures.Add(new ValidationFailure("columnName", $"Duplicate column: '{name}'."));
        }

        foreach (var r in allow)
        {
            if (!seen.Contains(r))
                failures.Add(new ValidationFailure("", $"Missing column: '{r}'."));
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);
    }

    private static CustomerColumnPermissionsResponse FullAllowResponse()
    {
        var cols = SchemaConstants.GetAllCustomerColumnNames()
            .Select(n => new CustomerColumnPermissionItemDto { Name = n, CanView = true, CanEdit = true })
            .ToList();
        return new CustomerColumnPermissionsResponse { Columns = cols };
    }

    private async Task<CustomerColumnPermissionsResponse> BuildStaffResponseAsync(Guid userId,
        CancellationToken cancellationToken)
    {
        var allow = SchemaConstants.GetAllCustomerColumnNames().ToHashSet(StringComparer.Ordinal);
        var rows = await _columnPermissions.ListByUserAndTableAsync(userId, SchemaConstants.CustomersTable, cancellationToken);
        var dict = rows.ToDictionary(r => r.ColumnName, r => (r.CanView, r.CanEdit), StringComparer.Ordinal);

        var columns = allow.OrderBy(n => n, StringComparer.Ordinal).Select(name =>
        {
            if (dict.TryGetValue(name, out var t))
                return new CustomerColumnPermissionItemDto { Name = name, CanView = t.CanView, CanEdit = t.CanEdit };
            return new CustomerColumnPermissionItemDto { Name = name, CanView = false, CanEdit = false };
        }).ToList();

        return new CustomerColumnPermissionsResponse { Columns = columns };
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
