using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class CreateProductGroupRequestValidator : AbstractValidator<CreateProductGroupRequest>
{
    public CreateProductGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
