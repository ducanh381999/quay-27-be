using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Quay27_Be.Hubs;

[Authorize]
public class CustomerSheetHub : Hub
{
    public const string CustomerSheetChangedEvent = "CustomerSheetChanged";

    public static string GroupNameForSheet(DateOnly sheetDate) => $"sheet:{sheetDate:yyyy-MM-dd}";

    /// <summary>Subscribe to all changes for customers on this sheet date (any queue filter refetches with their API params).</summary>
    public Task JoinSheet(string sheetDate)
    {
        if (!DateOnly.TryParse(sheetDate, out var d))
            throw new HubException("Invalid sheetDate.");
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForSheet(d));
    }

    public Task LeaveSheet(string sheetDate)
    {
        if (!DateOnly.TryParse(sheetDate, out var d))
            return Task.CompletedTask;
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForSheet(d));
    }
}
