using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class ApplyPriceFormulaRequestValidator : AbstractValidator<ApplyPriceFormulaRequest>
{
    public ApplyPriceFormulaRequestValidator()
    {
        RuleFor(x => x.ApplyTo).NotEmpty().Must(x => x is "all" or "single");
        RuleFor(x => x.ProductId)
            .NotNull()
            .When(x => x.ApplyTo == "single");
    }
}
