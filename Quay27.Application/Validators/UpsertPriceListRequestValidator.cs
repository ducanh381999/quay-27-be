using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class UpsertPriceListRequestValidator : AbstractValidator<UpsertPriceListRequest>
{
    private static readonly int[] ValidRoundTo = [1, 10, 100, 1000, 10000];

    public UpsertPriceListRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Status).NotEmpty().Must(x => x is "active" or "paused");
        RuleFor(x => x.StartAt).LessThanOrEqualTo(x => x.EndAt);
        RuleFor(x => x.SalesRuleMode).NotEmpty().Must(x => x is "allow" or "warn" or "restrict");

        RuleFor(x => x.Formula.Source).NotEmpty().Must(x => x is "costPrice" or "lastImportPrice" or "basePriceList");
        RuleFor(x => x.Formula.Operation).NotEmpty().Must(x => x is "add" or "subtract");
        RuleFor(x => x.Formula.Value).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Formula.Unit).NotEmpty().Must(x => x is "vnd" or "percent");
        RuleFor(x => x.Formula.RoundTo).Must(x => ValidRoundTo.Contains(x));

        RuleFor(x => x.Scope.BranchIds).NotNull();
        RuleFor(x => x.Scope.CustomerGroupIds).NotNull();
        RuleFor(x => x.Scope.CashierIds).NotNull();
    }
}
