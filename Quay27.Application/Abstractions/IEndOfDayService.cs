namespace Quay27.Application.Abstractions;

public interface IEndOfDayService
{
    Task<int> RollUnqueuedCustomersToNextDayAsync(CancellationToken cancellationToken = default);
}
