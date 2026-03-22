using Quay27.Application.Queues;

namespace Quay27.Application.Abstractions;

public interface IQueueService
{
    Task<IReadOnlyList<QueueDto>> ListActiveAsync(CancellationToken cancellationToken = default);
}
