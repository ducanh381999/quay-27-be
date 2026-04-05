using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class SheetPickerDraftStaffNameRepository : ISheetPickerDraftStaffNameRepository
{
    private readonly ApplicationDbContext _db;

    public SheetPickerDraftStaffNameRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> ListOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SheetPickerDraftStaffNames.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceAllAsync(IReadOnlyList<string> displayNames, CancellationToken cancellationToken = default)
    {
        var existing = await _db.SheetPickerDraftStaffNames.ToListAsync(cancellationToken);
        _db.SheetPickerDraftStaffNames.RemoveRange(existing);

        var order = 0;
        foreach (var name in displayNames)
        {
            _db.SheetPickerDraftStaffNames.Add(new SheetPickerDraftStaffName
            {
                DisplayName = name,
                SortOrder = order++
            });
        }
    }
}
