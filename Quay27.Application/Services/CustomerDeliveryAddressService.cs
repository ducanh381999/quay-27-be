using FluentValidation;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.CustomerProfiles;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class CustomerDeliveryAddressService : ICustomerDeliveryAddressService
{
    private readonly ICustomerProfileRepository _profiles;
    private readonly ICustomerDeliveryAddressRepository _addresses;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCustomerDeliveryAddressRequest> _createValidator;
    private readonly IValidator<PatchCustomerDeliveryAddressRequest> _patchValidator;

    public CustomerDeliveryAddressService(
        ICustomerProfileRepository profiles,
        ICustomerDeliveryAddressRepository addresses,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IValidator<CreateCustomerDeliveryAddressRequest> createValidator,
        IValidator<PatchCustomerDeliveryAddressRequest> patchValidator)
    {
        _profiles = profiles;
        _addresses = addresses;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _patchValidator = patchValidator;
    }

    public async Task<IReadOnlyList<CustomerDeliveryAddressDto>> ListAsync(Guid customerProfileId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await EnsureCustomerExistsAsync(customerProfileId, cancellationToken);
        var list = await _addresses.ListByCustomerAsync(customerProfileId, cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<CustomerDeliveryAddressDto> CreateAsync(Guid customerProfileId,
        CreateCustomerDeliveryAddressRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureCustomerExistsAsync(customerProfileId, cancellationToken);
        var now = DateTime.UtcNow;
        var entity = new CustomerDeliveryAddress
        {
            Id = Guid.NewGuid(),
            CustomerProfileId = customerProfileId,
            AddressName = request.AddressName.Trim(),
            RecipientName = request.RecipientName.Trim(),
            Phone = request.Phone.Trim(),
            AddressLine = request.AddressLine.Trim(),
            ProvinceCity = request.ProvinceCity.Trim(),
            Ward = request.Ward.Trim(),
            CreatedAtUtc = now,
            CreatedBy = _currentUser.Username,
            IsDeleted = false,
        };
        await _addresses.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CustomerDeliveryAddressDto> PatchAsync(Guid customerProfileId, Guid addressId,
        PatchCustomerDeliveryAddressRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await _patchValidator.ValidateAndThrowAsync(request, cancellationToken);
        var item = await _addresses.GetTrackedByIdAsync(customerProfileId, addressId, cancellationToken);
        if (item is null)
            throw new NotFoundException("Delivery address not found.");
        if (request.AddressName is not null) item.AddressName = request.AddressName.Trim();
        if (request.RecipientName is not null) item.RecipientName = request.RecipientName.Trim();
        if (request.Phone is not null) item.Phone = request.Phone.Trim();
        if (request.AddressLine is not null) item.AddressLine = request.AddressLine.Trim();
        if (request.ProvinceCity is not null) item.ProvinceCity = request.ProvinceCity.Trim();
        if (request.Ward is not null) item.Ward = request.Ward.Trim();
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedBy = _currentUser.Username;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(Guid customerProfileId, Guid addressId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _addresses.GetTrackedByIdAsync(customerProfileId, addressId, cancellationToken);
        if (item is null)
            return;
        item.IsDeleted = true;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedBy = _currentUser.Username;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCustomerExistsAsync(Guid customerProfileId, CancellationToken cancellationToken)
    {
        if (await _profiles.GetByIdAsync(customerProfileId, cancellationToken) is null)
            throw new NotFoundException("Customer profile not found.");
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }

    private static CustomerDeliveryAddressDto Map(CustomerDeliveryAddress x) =>
        new(x.Id, x.AddressName, x.RecipientName, x.Phone, x.AddressLine, x.ProvinceCity, x.Ward, x.CreatedAtUtc);
}
