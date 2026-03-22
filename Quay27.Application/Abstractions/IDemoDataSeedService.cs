using Quay27.Application.Setup;

namespace Quay27.Application.Abstractions;

public interface IDemoDataSeedService
{
    Task<DemoSeedResponse> SeedDemoDataAsync(CancellationToken cancellationToken = default);
}
