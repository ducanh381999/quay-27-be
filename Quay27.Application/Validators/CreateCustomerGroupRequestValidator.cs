using FluentValidation;
using Quay27.Application.CustomerGroups;

namespace Quay27.Application.Validators;

public class CreateCustomerGroupRequestValidator : AbstractValidator<CreateCustomerGroupRequest>
{
    public CreateCustomerGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
    }
}
