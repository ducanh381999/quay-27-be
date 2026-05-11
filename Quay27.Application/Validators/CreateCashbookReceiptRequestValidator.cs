using FluentValidation;
using Quay27.Application.Cashbook;

namespace Quay27.Application.Validators;

public sealed class CreateCashbookReceiptRequestValidator : AbstractValidator<CreateCashbookReceiptRequest>
{
    private static readonly string[] FundTypes = ["cash", "bank", "ewallet"];
    private static readonly string[] Scopes = ["other", "customer", "supplier"];
    private static readonly string[] DebtModes =
        ["include_in_debt", "exclude_from_debt", "no_debt", "not_applicable"];

    public CreateCashbookReceiptRequestValidator()
    {
        RuleFor(x => x.PaymentCategoryId).GreaterThan(0);
        RuleFor(x => x.CollectorUserId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.FundType).Must(x => FundTypes.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("FundType không hợp lệ.");
        RuleFor(x => x.CounterpartyScope).Must(x => Scopes.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("CounterpartyScope không hợp lệ.");
        RuleFor(x => x.PartnerDebtMode).Must(x => DebtModes.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("PartnerDebtMode không hợp lệ.");
    }
}
