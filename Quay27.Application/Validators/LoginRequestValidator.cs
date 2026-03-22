using FluentValidation;
using Quay27.Application.Auth;

namespace Quay27.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}
