namespace Quay27.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>When true (non-admin), user may DELETE customer rows not yet on Quầy 27 queue.</summary>
    public bool CanDeleteCustomers { get; set; }

    public DateTime CreatedDate { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<ColumnPermission> ColumnPermissions { get; set; } = new List<ColumnPermission>();
}
