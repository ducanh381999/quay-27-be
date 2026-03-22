namespace Quay27.Application.Customers;

public sealed class CustomerAuditLogEntryDto
{
    public Guid Id { get; init; }
    public string TableName { get; init; } = string.Empty;
    public Guid RecordId { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string ChangedBy { get; init; } = string.Empty;
    public DateTime ChangedDate { get; init; }
}
