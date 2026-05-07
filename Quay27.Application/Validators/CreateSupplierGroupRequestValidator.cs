using FluentValidation;
using Quay27.Application.Suppliers;

namespace Quay27.Application.Validators;

public class CreateSupplierGroupRequestValidator : AbstractValidator<CreateSupplierGroupRequest>
{
    public CreateSupplierGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
    }
}

public class UpdateSupplierGroupRequestValidator : AbstractValidator<UpdateSupplierGroupRequest>
{
    public UpdateSupplierGroupRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(256).When(x => x.Name is not null);
    }
}
