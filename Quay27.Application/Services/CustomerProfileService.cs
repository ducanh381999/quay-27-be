using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.CustomerProfiles;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class CustomerProfileService : ICustomerProfileService
{
    private readonly ICustomerProfileRepository _profiles;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerProfileService(
        ICustomerProfileRepository profiles,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _profiles = profiles;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CustomerProfileDto>> ListAsync(string? search = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var list = await _profiles.ListAsync(search, cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<CustomerProfileDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _profiles.GetByIdAsync(id, cancellationToken);
        if (item is null)
            throw new NotFoundException("Customer profile not found.");
        return Map(item);
    }

    public async Task<CustomerProfileDto> CreateAsync(CreateCustomerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var username = _currentUser.Username;
        var now = DateTime.UtcNow;

        var entity = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            CustomerCode = await GenerateCustomerCodeAsync(cancellationToken),
            CustomerName = request.CustomerName.Trim(),
            Phone1 = request.Phone1.Trim(),
            Phone2 = request.Phone2.Trim(),
            Birthday = request.Birthday,
            Gender = request.Gender.Trim(),
            Email = request.Email.Trim(),
            Facebook = request.Facebook.Trim(),
            Address = request.Address.Trim(),
            ProvinceCity = request.ProvinceCity.Trim(),
            Ward = request.Ward.Trim(),
            CustomerGroup = request.CustomerGroup.Trim(),
            Note = request.Note.Trim(),
            BuyerType = string.IsNullOrWhiteSpace(request.BuyerType) ? "individual" : request.BuyerType.Trim(),
            BuyerName = request.BuyerName.Trim(),
            TaxCode = request.TaxCode.Trim(),
            InvoiceAddress = request.InvoiceAddress.Trim(),
            InvoiceProvinceCity = request.InvoiceProvinceCity.Trim(),
            InvoiceWard = request.InvoiceWard.Trim(),
            IdentityNumber = request.IdentityNumber.Trim(),
            PassportNumber = request.PassportNumber.Trim(),
            InvoiceEmail = request.InvoiceEmail.Trim(),
            InvoicePhone = request.InvoicePhone.Trim(),
            BankName = request.BankName.Trim(),
            BankAccountNumber = request.BankAccountNumber.Trim(),
            CreatedDate = now,
            CreatedBy = username,
            IsDeleted = false,
        };

        await _profiles.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CustomerProfileDto> PatchAsync(Guid id, PatchCustomerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _profiles.GetTrackedByIdAsync(id, cancellationToken);
        if (item is null)
            throw new NotFoundException("Customer profile not found.");

        if (request.CustomerName is not null) item.CustomerName = request.CustomerName.Trim();
        if (request.Phone1 is not null) item.Phone1 = request.Phone1.Trim();
        if (request.Phone2 is not null) item.Phone2 = request.Phone2.Trim();
        if (request.Birthday is not null) item.Birthday = request.Birthday;
        if (request.Gender is not null) item.Gender = request.Gender.Trim();
        if (request.Email is not null) item.Email = request.Email.Trim();
        if (request.Facebook is not null) item.Facebook = request.Facebook.Trim();
        if (request.Address is not null) item.Address = request.Address.Trim();
        if (request.ProvinceCity is not null) item.ProvinceCity = request.ProvinceCity.Trim();
        if (request.Ward is not null) item.Ward = request.Ward.Trim();
        if (request.CustomerGroup is not null) item.CustomerGroup = request.CustomerGroup.Trim();
        if (request.Note is not null) item.Note = request.Note.Trim();
        if (request.BuyerType is not null) item.BuyerType = request.BuyerType.Trim();
        if (request.BuyerName is not null) item.BuyerName = request.BuyerName.Trim();
        if (request.TaxCode is not null) item.TaxCode = request.TaxCode.Trim();
        if (request.InvoiceAddress is not null) item.InvoiceAddress = request.InvoiceAddress.Trim();
        if (request.InvoiceProvinceCity is not null) item.InvoiceProvinceCity = request.InvoiceProvinceCity.Trim();
        if (request.InvoiceWard is not null) item.InvoiceWard = request.InvoiceWard.Trim();
        if (request.IdentityNumber is not null) item.IdentityNumber = request.IdentityNumber.Trim();
        if (request.PassportNumber is not null) item.PassportNumber = request.PassportNumber.Trim();
        if (request.InvoiceEmail is not null) item.InvoiceEmail = request.InvoiceEmail.Trim();
        if (request.InvoicePhone is not null) item.InvoicePhone = request.InvoicePhone.Trim();
        if (request.BankName is not null) item.BankName = request.BankName.Trim();
        if (request.BankAccountNumber is not null) item.BankAccountNumber = request.BankAccountNumber.Trim();

        item.UpdatedBy = _currentUser.Username;
        item.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var item = await _profiles.GetTrackedByIdAsync(id, cancellationToken);
        if (item is null)
            return;
        item.IsDeleted = true;
        item.UpdatedBy = _currentUser.Username;
        item.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }

    private async Task<string> GenerateCustomerCodeAsync(CancellationToken cancellationToken)
    {
        var prefix = $"KH{DateTime.UtcNow:yyyyMMdd}";
        for (var i = 1; i <= 9999; i++)
        {
            var code = $"{prefix}-{i:D4}";
            if (!await _profiles.CustomerCodeExistsAsync(code, cancellationToken))
                return code;
        }

        throw new ConflictException("Cannot generate customer code, please retry.");
    }

    private static CustomerProfileDto Map(CustomerProfile x) =>
        new()
        {
            Id = x.Id,
            CustomerCode = x.CustomerCode,
            CustomerName = x.CustomerName,
            Phone1 = x.Phone1,
            Phone2 = x.Phone2,
            Birthday = x.Birthday,
            Gender = x.Gender,
            Email = x.Email,
            Facebook = x.Facebook,
            Address = x.Address,
            ProvinceCity = x.ProvinceCity,
            Ward = x.Ward,
            CustomerGroup = x.CustomerGroup,
            Note = x.Note,
            BuyerType = x.BuyerType,
            BuyerName = x.BuyerName,
            TaxCode = x.TaxCode,
            InvoiceAddress = x.InvoiceAddress,
            InvoiceProvinceCity = x.InvoiceProvinceCity,
            InvoiceWard = x.InvoiceWard,
            IdentityNumber = x.IdentityNumber,
            PassportNumber = x.PassportNumber,
            InvoiceEmail = x.InvoiceEmail,
            InvoicePhone = x.InvoicePhone,
            BankName = x.BankName,
            BankAccountNumber = x.BankAccountNumber,
            CreatedDate = x.CreatedDate,
            CreatedBy = x.CreatedBy,
            UpdatedDate = x.UpdatedDate,
            UpdatedBy = x.UpdatedBy,
        };
}
