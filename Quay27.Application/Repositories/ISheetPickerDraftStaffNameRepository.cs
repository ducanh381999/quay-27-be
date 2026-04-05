namespace Quay27.Application.Repositories;

public interface ISheetPickerDraftStaffNameRepository
{
    Task<IReadOnlyList<string>> ListOrderedAsync(CancellationToken cancellationToken = default);

    Task ReplaceAllAsync(IReadOnlyList<string> displayNames, CancellationToken cancellationToken = default);
}
