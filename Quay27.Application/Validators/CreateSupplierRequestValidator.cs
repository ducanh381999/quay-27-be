using FluentValidation;
using Quay27.Application.Suppliers;

namespace Quay27.Application.Validators;

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).MaximumLength(32);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Email)
            .MaximumLength(256)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Region).MaximumLength(256);
        RuleFor(x => x.Ward).MaximumLength(256);
        RuleFor(x => x.CompanyName).MaximumLength(256);
        RuleFor(x => x.TaxCode).MaximumLength(32);
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(256).When(x => x.Name is not null);
        RuleFor(x => x.Phone).MaximumLength(32).When(x => x.Phone is not null);
        RuleFor(x => x.Email)
            .MaximumLength(256)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Region).MaximumLength(256).When(x => x.Region is not null);
        RuleFor(x => x.Ward).MaximumLength(256).When(x => x.Ward is not null);
        RuleFor(x => x.CompanyName).MaximumLength(256).When(x => x.CompanyName is not null);
        RuleFor(x => x.TaxCode).MaximumLength(32).When(x => x.TaxCode is not null);
    }
}
