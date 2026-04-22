using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class UpdateProductGroupRequestValidator : AbstractValidator<UpdateProductGroupRequest>
{
    public UpdateProductGroupRequestValidator()
    {
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(128);
    }
}
