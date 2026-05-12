using Microsoft.Extensions.Options;
using Quay27.Application.Abstractions;
using Quay27.Application.Repositories;
using Quay27_Be.Options;

namespace Quay27_Be.Background;

public sealed class CustomerGroupAutoSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<CustomerGroupAutoSyncOptions> _options;
    private readonly ILogger<CustomerGroupAutoSyncBackgroundService> _logger;

    public CustomerGroupAutoSyncBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<CustomerGroupAutoSyncOptions> options,
        ILogger<CustomerGroupAutoSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var opt = _options.Value;
            var interval = TimeSpan.FromMinutes(Math.Clamp(opt.IntervalMinutes, 5, 24 * 60));

            if (opt.Enabled)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var groups = scope.ServiceProvider.GetRequiredService<ICustomerGroupRepository>();
                    var sync = scope.ServiceProvider.GetRequiredService<ICustomerGroupMembershipSyncService>();
                    var candidates = await groups.ListForAutoMembershipSyncAsync(stoppingToken);
                    foreach (var g in candidates)
                    {
                        try
                        {
                            await sync.SyncAsync(g.Id, stoppingToken);
                        }
                        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogError(ex, "Auto membership sync failed for customer group {GroupId} {Name}.", g.Id,
                                g.Name);
                        }
                    }

                    if (candidates.Count > 0)
                        _logger.LogInformation("Customer group auto sync finished for {Count} group(s).", candidates.Count);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Customer group auto sync batch failed.");
                }
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
