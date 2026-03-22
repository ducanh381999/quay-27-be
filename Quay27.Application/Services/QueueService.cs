using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Queues;
using Quay27.Application.Repositories;

namespace Quay27.Application.Services;

public class QueueService : IQueueService
{
    private readonly IQueueRepository _queues;
    private readonly ICurrentUser _currentUser;

    public QueueService(IQueueRepository queues, ICurrentUser currentUser)
    {
        _queues = queues;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<QueueDto>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
            throw new ForbiddenException("Authentication required.");

        return await _queues.ListActiveAsync(cancellationToken);
    }
}
