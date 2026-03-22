using FluentValidation;
using Quay27.Application.Users;

namespace Quay27.Application.Validators;

public class PatchUserRequestValidator : AbstractValidator<PatchUserRequest>
{
    public PatchUserRequestValidator()
    {
        RuleFor(x => x.Username).MaximumLength(128).When(x => x.Username is not null);
        RuleFor(x => x.FullName).MaximumLength(256).When(x => x.FullName is not null);
        RuleFor(x => x.RoleNames).NotEmpty().When(x => x.RoleNames is not null);
    }
}
