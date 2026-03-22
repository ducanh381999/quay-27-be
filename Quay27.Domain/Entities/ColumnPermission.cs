namespace Quay27.Domain.Entities;

public class ColumnPermission
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }

    public User User { get; set; } = null!;
}
