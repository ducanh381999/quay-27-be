using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Abstractions;
using Quay27.Application.Orders;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class SaleChannelService : ISaleChannelService
{
    private readonly ISaleChannelRepository _channels;
    private readonly IValidator<CreateSaleChannelRequest> _createValidator;
    private readonly IUnitOfWork _unitOfWork;

    public SaleChannelService(
        ISaleChannelRepository channels,
        IValidator<CreateSaleChannelRequest> createValidator,
        IUnitOfWork unitOfWork)
    {
        _channels = channels;
        _createValidator = createValidator;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SaleChannelDto>> ListAsync(bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var list = includeInactive
            ? await _channels.ListAllOrderedAsync(cancellationToken)
            : await _channels.ListActiveOrderedAsync(cancellationToken);

        return list
            .Select(x => new SaleChannelDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
            }).ToList();
    }

    public async Task<SaleChannelDto> CreateAsync(CreateSaleChannelRequest request,
        CancellationToken cancellationToken = default)
    {
        var vr = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid) throw new ValidationException(vr.Errors);

        var trimmed = request.Name.Trim();
        if (await _channels.ExistsNameAsync(trimmed, null, cancellationToken))
            throw new ValidationException(new[]
            {
                new ValidationFailure("name", $"Tên kênh bán đã tồn tại: {trimmed}"),
            });

        var entity = new SaleChannel
        {
            Id = Guid.NewGuid(),
            Name = trimmed,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
        };
        await _channels.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SaleChannelDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
        };
    }
}
