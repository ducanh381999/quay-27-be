namespace Quay27.Application.Abstractions;

public sealed record CustomerSheetChangeNotification(
    DateOnly SheetDate,
    Guid CustomerId,
    string ChangeType);

/// <summary>Pushes customer sheet updates to SignalR groups (best-effort; must not throw to callers).</summary>
public interface ICustomerSheetRealtimeNotifier
{
    Task NotifyAsync(CustomerSheetChangeNotification notification, CancellationToken cancellationToken = default);
}
