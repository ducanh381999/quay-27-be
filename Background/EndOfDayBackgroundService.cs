using Quay27.Application.Abstractions;

namespace Quay27_Be.Background;

public class EndOfDayBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EndOfDayBackgroundService> _logger;

    public EndOfDayBackgroundService(IServiceProvider serviceProvider, ILogger<EndOfDayBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextLocalMidnight();
            _logger.LogInformation("End of day job scheduled in {Delay}", delay);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var endOfDay = scope.ServiceProvider.GetRequiredService<IEndOfDayService>();
                await endOfDay.RollUnqueuedCustomersToNextDayAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "End of day background execution failed.");
            }
        }
    }

    private static TimeSpan ComputeDelayUntilNextLocalMidnight()
    {
        var now = DateTimeOffset.Now;
        var nextLocalMidnight = now.Date.AddDays(1);
        var next = new DateTimeOffset(nextLocalMidnight);
        var delay = next - now;
        return delay <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : delay;
    }
}
