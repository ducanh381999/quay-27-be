using FluentValidation;
using Quay27.Application.CustomerProfiles;

namespace Quay27.Application.Validators;

public class CreateCustomerProfileRequestValidator : AbstractValidator<CreateCustomerProfileRequest>
{
    public CreateCustomerProfileRequestValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone1).MaximumLength(32);
        RuleFor(x => x.Phone2).MaximumLength(32);
        RuleFor(x => x.Gender).MaximumLength(32);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.Facebook).MaximumLength(512);
        RuleFor(x => x.ProvinceCity).MaximumLength(128);
        RuleFor(x => x.Ward).MaximumLength(128);
        RuleFor(x => x.CustomerGroup).MaximumLength(128);
        RuleFor(x => x.BuyerType).MaximumLength(32);
        RuleFor(x => x.BuyerName).MaximumLength(256);
        RuleFor(x => x.TaxCode).MaximumLength(64);
        RuleFor(x => x.InvoiceProvinceCity).MaximumLength(128);
        RuleFor(x => x.InvoiceWard).MaximumLength(128);
        RuleFor(x => x.IdentityNumber).MaximumLength(32);
        RuleFor(x => x.PassportNumber).MaximumLength(32);
        RuleFor(x => x.InvoiceEmail).MaximumLength(256);
        RuleFor(x => x.InvoicePhone).MaximumLength(32);
        RuleFor(x => x.BankName).MaximumLength(128);
        RuleFor(x => x.BankAccountNumber).MaximumLength(64);
    }
}
