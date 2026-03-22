using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class CustomerVersionRepository : ICustomerVersionRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerVersionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(CustomerVersion version, CancellationToken cancellationToken = default) =>
        _db.CustomerVersions.AddAsync(version, cancellationToken).AsTask();
}
