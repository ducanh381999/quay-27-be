namespace Quay27.Application.Repositories;

public interface ISheetPickerMemberRepository
{
    Task<IReadOnlyList<Guid>> ListUserIdsAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    void Add(Guid userId);
}
