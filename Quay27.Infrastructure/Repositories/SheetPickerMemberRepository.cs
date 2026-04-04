using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class SheetPickerMemberRepository : ISheetPickerMemberRepository
{
    private readonly ApplicationDbContext _db;

    public SheetPickerMemberRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Guid>> ListUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.SheetPickerMembers.AsNoTracking()
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.SheetPickerMembers.ToListAsync(cancellationToken);
        _db.SheetPickerMembers.RemoveRange(rows);
    }

    public void Add(Guid userId) =>
        _db.SheetPickerMembers.Add(new SheetPickerMember { UserId = userId });
}
