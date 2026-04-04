namespace Quay27.Application.Users;

/// <summary>Minimal user info for sheet dropdowns (NV soạn, NV lắp CM).</summary>
public sealed class UserPickerDto
{
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}

public sealed class UserSummaryDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool CanDeleteCustomers { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public sealed class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<string> RoleNames { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; } = true;
}

public sealed class PatchUserRequest
{
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public bool? IsActive { get; set; }
    public bool? CanDeleteCustomers { get; set; }
    public IReadOnlyList<string>? RoleNames { get; set; }
}

public sealed class SheetPickerMembersPutRequest
{
    public IReadOnlyList<Guid> UserIds { get; set; } = Array.Empty<Guid>();
}

public sealed class ResetUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class CustomerColumnPermissionItemDto
{
    public string Name { get; init; } = string.Empty;
    public bool CanView { get; init; }
    public bool CanEdit { get; init; }
}

public sealed class CustomerColumnPermissionsResponse
{
    public IReadOnlyList<CustomerColumnPermissionItemDto> Columns { get; init; } = Array.Empty<CustomerColumnPermissionItemDto>();
}

/// <summary>PUT body item: JSON <c>columnName</c>, <c>canView</c>, <c>canEdit</c>.</summary>
public sealed class CustomerColumnPermissionInput
{
    public string ColumnName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
}
