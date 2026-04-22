using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class PriceListItemsQueryValidator : AbstractValidator<PriceListItemsQuery>
{
    private static readonly string[] AllowedOperators = ["none", "lt", "lte", "eq", "gt", "gte"];
    private static readonly string[] AllowedComparePrices = ["none", "costPrice", "lastImportPrice"];

    public PriceListItemsQueryValidator()
    {
        RuleFor(x => x.PriceListIds).NotNull();

        RuleFor(x => x.PriceOperator)
            .Must(x => string.IsNullOrWhiteSpace(x) || AllowedOperators.Contains(x))
            .WithMessage("priceOperator is invalid.");

        RuleFor(x => x.ComparePrice)
            .Must(x => string.IsNullOrWhiteSpace(x) || AllowedComparePrices.Contains(x))
            .WithMessage("comparePrice is invalid.");

        RuleFor(x => x.CompareValue)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CompareValue.HasValue);

        RuleFor(x => x)
            .Must(x =>
            {
                var op = x.PriceOperator ?? "none";
                var source = x.ComparePrice ?? "none";
                if (op == "none")
                {
                    return source == "none" && x.CompareValue is null;
                }

                return source != "none" && x.CompareValue.HasValue;
            })
            .WithMessage("priceOperator requires comparePrice and compareValue.");
    }
}
