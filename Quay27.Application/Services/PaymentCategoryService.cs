using Quay27.Application.Abstractions;
using Quay27.Application.Cashbook;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;

namespace Quay27.Application.Services;

public sealed class PaymentCategoryService : IPaymentCategoryService
{
    private readonly IPaymentCategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public PaymentCategoryService(IPaymentCategoryRepository categories, ICurrentUser currentUser)
    {
        _categories = categories;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PaymentCategoryDto>> ListAsync(string? kind,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
            throw new ForbiddenException("Authentication required.");

        var rows = await _categories.ListAsync(kind, cancellationToken);
        return rows.Select(x => new PaymentCategoryDto(x.Id, x.Code, x.Name, x.Kind)).ToList();
    }
}
