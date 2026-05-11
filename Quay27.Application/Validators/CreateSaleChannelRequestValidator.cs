using FluentValidation;
using Quay27.Application.Orders;

namespace Quay27.Application.Validators;

public sealed class CreateSaleChannelRequestValidator : AbstractValidator<CreateSaleChannelRequest>
{
    public CreateSaleChannelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
    }
}
