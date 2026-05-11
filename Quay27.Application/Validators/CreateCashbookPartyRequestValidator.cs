using FluentValidation;
using Quay27.Application.Cashbook;

namespace Quay27.Application.Validators;

public sealed class CreateCashbookPartyRequestValidator : AbstractValidator<CreateCashbookPartyRequest>
{
    public CreateCashbookPartyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Address).MaximumLength(512);
        RuleFor(x => x.Province).MaximumLength(128);
        RuleFor(x => x.Ward).MaximumLength(128);
    }
}
