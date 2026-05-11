using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public sealed class PaymentCategoryRepository : IPaymentCategoryRepository
{
    private readonly ApplicationDbContext _db;

    public PaymentCategoryRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<PaymentCategory>> ListAsync(string? kind,
        CancellationToken cancellationToken = default)
    {
        var q = _db.PaymentCategories.AsNoTracking().OrderBy(x => x.Id);
        if (!string.IsNullOrWhiteSpace(kind))
        {
            var k = kind.Trim();
            return await q.Where(x => x.Kind == k).ToListAsync(cancellationToken);
        }

        return await q.ToListAsync(cancellationToken);
    }

    public Task<PaymentCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.PaymentCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PaymentCategory?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.PaymentCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
}
