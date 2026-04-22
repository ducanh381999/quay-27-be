using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class UpdateProductStatusRequestValidator : AbstractValidator<UpdateProductStatusRequest>
{
    public UpdateProductStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty().Must(x => x is "active" or "inactive");
    }
}
