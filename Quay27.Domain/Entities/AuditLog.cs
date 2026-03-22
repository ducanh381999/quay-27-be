namespace Quay27.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedDate { get; set; }
}
