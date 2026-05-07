using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;
using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class SupplierGroupService : ISupplierGroupService
{
    private readonly ISupplierGroupRepository _groups;
    private readonly ISupplierRepository _suppliers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SupplierGroupService> _logger;

    public SupplierGroupService(
        ISupplierGroupRepository groups,
        ISupplierRepository suppliers,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<SupplierGroupService> logger)
    {
        _groups = groups;
        _suppliers = suppliers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public Task<IReadOnlyList<SupplierGroupDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _groups.ListAsync(cancellationToken);
    }

    public async Task<SupplierGroupDto> CreateAsync(CreateSupplierGroupRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var name = request.Name.Trim();
        if (await _groups.NameExistsAsync(name, excludeId: null, cancellationToken))
            throw new ConflictException($"Nhóm nhà cung cấp '{name}' đã tồn tại.");

        var entity = new SupplierGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            Notes = request.Notes?.Trim(),
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.Username,
        };
        await _groups.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SupplierGroup {Id} created by {User}", entity.Id, _currentUser.Username);
        return new SupplierGroupDto(entity.Id, entity.Name, entity.Notes, 0, entity.CreatedDate, entity.CreatedBy);
    }

    public async Task<SupplierGroupDto> UpdateAsync(Guid id, UpdateSupplierGroupRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var entity = await _groups.GetTrackedAsync(id, cancellationToken)
            ?? throw new NotFoundException("Nhóm nhà cung cấp không tồn tại.");

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrEmpty(name))
                throw new ConflictException("Tên nhóm không được để trống.");
            if (!string.Equals(name, entity.Name, StringComparison.OrdinalIgnoreCase)
                && await _groups.NameExistsAsync(name, excludeId: id, cancellationToken))
                throw new ConflictException($"Nhóm nhà cung cấp '{name}' đã tồn tại.");
            entity.Name = name;
        }

        if (request.Notes is not null)
            entity.Notes = request.Notes.Trim();

        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.Username;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var count = await _suppliers.HasSuppliersInGroupAsync(id, cancellationToken) ? 1 : 0;
        return new SupplierGroupDto(entity.Id, entity.Name, entity.Notes, count, entity.CreatedDate, entity.CreatedBy);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (await _suppliers.HasSuppliersInGroupAsync(id, cancellationToken))
            throw new ConflictException("Không thể xóa nhóm khi vẫn còn nhà cung cấp thuộc nhóm này.");

        var ok = await _groups.SoftDeleteAsync(id, _currentUser.Username, cancellationToken);
        if (!ok)
            throw new NotFoundException("Nhóm nhà cung cấp không tồn tại.");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
