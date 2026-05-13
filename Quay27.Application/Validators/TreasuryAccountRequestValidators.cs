using FluentValidation;
using Quay27.Application.Settings;
using Quay27.Domain.Constants;

namespace Quay27.Application.Validators;

public sealed class CreateTreasuryAccountRequestValidator : AbstractValidator<CreateTreasuryAccountRequest>
{
    public CreateTreasuryAccountRequestValidator()
    {
        RuleFor(x => x.Kind).NotEmpty().MaximumLength(32)
            .Must(k => string.Equals(k, "bank", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(k, "ewallet", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Kind phải là bank hoặc ewallet.");
        RuleFor(x => x.ProviderCode).NotEmpty().MaximumLength(128);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.AccountHolderName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Note).MaximumLength(4000);
        RuleFor(x => x.ScopeKind).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ScopeKind)
            .Must(s => string.Equals(s, TreasuryConstants.ScopeSystemWide, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(s, TreasuryConstants.ScopeBranch, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Phạm vi không hợp lệ.");
    }
}

public sealed class PatchTreasuryAccountRequestValidator : AbstractValidator<PatchTreasuryAccountRequest>
{
    public PatchTreasuryAccountRequestValidator()
    {
        RuleFor(x => x.ProviderCode).MaximumLength(128).When(x => x.ProviderCode is not null);
        RuleFor(x => x.AccountNumber).MaximumLength(64).When(x => x.AccountNumber is not null);
        RuleFor(x => x.AccountHolderName).MaximumLength(256).When(x => x.AccountHolderName is not null);
        RuleFor(x => x.Note).MaximumLength(4000).When(x => x.Note is not null);
        RuleFor(x => x.ScopeKind)
            .MaximumLength(32)
            .Must(s => s is null
                       || string.Equals(s, TreasuryConstants.ScopeSystemWide, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(s, TreasuryConstants.ScopeBranch, StringComparison.OrdinalIgnoreCase))
            .When(x => x.ScopeKind is not null)
            .WithMessage("Phạm vi không hợp lệ.");
    }
}
