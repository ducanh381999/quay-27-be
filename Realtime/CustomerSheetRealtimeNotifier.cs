using Microsoft.AspNetCore.SignalR;
using Quay27.Application.Abstractions;
using Quay27_Be.Hubs;

namespace Quay27_Be.Realtime;

public class CustomerSheetRealtimeNotifier : ICustomerSheetRealtimeNotifier
{
    private readonly IHubContext<CustomerSheetHub> _hub;
    private readonly ILogger<CustomerSheetRealtimeNotifier> _logger;

    public CustomerSheetRealtimeNotifier(
        IHubContext<CustomerSheetHub> hub,
        ILogger<CustomerSheetRealtimeNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyAsync(CustomerSheetChangeNotification notification, CancellationToken cancellationToken = default)
    {
        var group = CustomerSheetHub.GroupNameForSheet(notification.SheetDate);
        try
        {
            await _hub.Clients.Group(group).SendAsync(
                CustomerSheetHub.CustomerSheetChangedEvent,
                new
                {
                    sheetDate = notification.SheetDate.ToString("yyyy-MM-dd"),
                    customerId = notification.CustomerId,
                    changeType = notification.ChangeType
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR CustomerSheetChanged to group {Group} failed", group);
        }
    }
}
