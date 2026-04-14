using Quay27.Application.Users;

namespace Quay27.Application.Abstractions;

public interface IUserAdminService
{
    /// <summary>Active users for customer sheet staff pickers (any authenticated caller).</summary>
    Task<IReadOnlyList<UserPickerDto>> ListForSheetPickersAsync(CancellationToken cancellationToken = default);

    /// <summary>Configured NV soạn names (admin screen), ordered.</summary>
    Task<IReadOnlyList<string>> ListSheetPickerDraftNamesAsync(CancellationToken cancellationToken = default);

    Task ReplaceSheetPickerDraftNamesAsync(IReadOnlyList<string> displayNames,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserSummaryDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserSummaryDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto> PatchAsync(Guid id, PatchUserRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
