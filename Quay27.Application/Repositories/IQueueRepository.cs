using Quay27.Application.Queues;

namespace Quay27.Application.Repositories;

public interface IQueueRepository
{
    Task<IReadOnlyList<QueueDto>> ListActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int queueId, CancellationToken cancellationToken = default);
}
