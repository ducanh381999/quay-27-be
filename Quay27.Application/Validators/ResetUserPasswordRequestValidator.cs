using FluentValidation;
using Quay27.Application.Users;

namespace Quay27.Application.Validators;

public class ResetUserPasswordRequestValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}
