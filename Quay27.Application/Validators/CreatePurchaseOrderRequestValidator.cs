using FluentValidation;
using Quay27.Application.Orders;

namespace Quay27.Application.Validators;

public sealed class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    public CreatePurchaseOrderRequestValidator(CreateOrderItemRequestValidator lineValidator)
    {
        RuleFor(x => x.PaymentMethod)
            .Must(pm => string.Equals(pm, "cash", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Chỉ hỗ trợ tiền mặt.");
        RuleFor(x => x.Note).MaximumLength(4000).When(x => x.Note != null);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(lineValidator);

        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClientDiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClientSubtotal).GreaterThanOrEqualTo(0);
    }
}
