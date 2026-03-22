using FluentValidation;
using Quay27.Application.Users;

namespace Quay27.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
        RuleFor(x => x.RoleNames).NotNull();
    }
}
