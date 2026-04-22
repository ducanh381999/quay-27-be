using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.CustomerGroups;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class CustomerGroupService : ICustomerGroupService
{
    private readonly ICustomerGroupRepository _groups;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerGroupService(
        ICustomerGroupRepository groups,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _groups = groups;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CustomerGroupDto>> ListAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var items = await _groups.ListAsync(search, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<CustomerGroupTreeDto>> ListTreeAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var items = await _groups.ListAllAsync(cancellationToken);
        return items
            .Select(x => new CustomerGroupTreeDto
            {
                Id = x.Id,
                Name = x.Name,
                Children = Array.Empty<CustomerGroupTreeDto>()
            })
            .ToList();
    }

    public async Task<CustomerGroupDto> CreateAsync(
        CreateCustomerGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var name = request.Name.Trim();
        if (await _groups.NameExistsAsync(name, null, cancellationToken))
            throw new ConflictException("Customer group name already exists.");

        var entity = new CustomerGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = NormalizeNullable(request.Description),
            CreatedDate = DateTime.UtcNow
        };
        await _groups.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CustomerGroupDto> UpdateAsync(
        Guid id,
        UpdateCustomerGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _groups.GetTrackedByIdAsync(id, cancellationToken);
        if (item is null)
            throw new NotFoundException("Customer group not found.");

        var name = request.Name.Trim();
        if (await _groups.NameExistsAsync(name, id, cancellationToken))
            throw new ConflictException("Customer group name already exists.");

        item.Name = name;
        item.Description = NormalizeNullable(request.Description);
        item.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _groups.GetTrackedByIdAsync(id, cancellationToken);
        if (item is null)
            return;

        item.IsDeleted = true;
        item.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CustomerGroupDto Map(CustomerGroup x) =>
        new()
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            CreatedDate = x.CreatedDate,
            UpdatedDate = x.UpdatedDate
        };
}
