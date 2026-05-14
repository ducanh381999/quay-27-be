using FluentValidation;
using Quay27.Application.Cashbook;

namespace Quay27.Application.Validators;

public sealed class PatchCashbookEntryRequestValidator : AbstractValidator<PatchCashbookEntryRequest>
{
    public PatchCashbookEntryRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasAnyChange)
            .WithMessage("Không có trường nào được gửi để cập nhật.");
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.PaymentCategoryId).GreaterThan(0).When(x => x.PaymentCategoryId.HasValue);
        RuleFor(x => x.FundType)
            .Must(f => f is null or "cash" or "bank" or "ewallet")
            .When(x => !string.IsNullOrWhiteSpace(x.FundType))
            .WithMessage("FundType không hợp lệ.");
        RuleFor(x => x.CounterpartyScope)
            .Must(s => s is null or "other" or "customer" or "supplier")
            .When(x => !string.IsNullOrWhiteSpace(x.CounterpartyScope))
            .WithMessage("CounterpartyScope không hợp lệ.");
        RuleForEach(x => x.Allocations).ChildRules(a =>
        {
            a.RuleFor(i => i.TargetId).NotEmpty();
            a.RuleFor(i => i.Amount).GreaterThan(0);
        }).When(x => x.Allocations is { Count: > 0 });
    }

    private static bool HasAnyChange(PatchCashbookEntryRequest x) =>
        x.OccurredAtUtc.HasValue ||
        x.PaymentCategoryId.HasValue ||
        x.CollectorUserId.HasValue ||
        !string.IsNullOrWhiteSpace(x.CounterpartyScope) ||
        x.CashbookPartyId.HasValue ||
        x.CounterpartyDisplayName != null ||
        x.Amount.HasValue ||
        x.Note != null ||
        x.AffectsBusinessResult.HasValue ||
        !string.IsNullOrWhiteSpace(x.FundType) ||
        x.StaffUserId.HasValue ||
        (x.Allocations is { Count: > 0 });
}
