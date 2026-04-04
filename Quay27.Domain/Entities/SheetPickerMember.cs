namespace Quay27.Domain.Entities;

/// <summary>Staff included in NV soạn sheet pickers (admins always included by role).</summary>
public class SheetPickerMember
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
