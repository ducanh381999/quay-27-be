using FluentValidation;
using Quay27.Application.Orders;

namespace Quay27.Application.Validators;

public sealed class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductCode).MaximumLength(64);
        RuleFor(x => x.ProductName).MaximumLength(512).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Note).MaximumLength(2000).When(x => x.Note != null);
    }
}
