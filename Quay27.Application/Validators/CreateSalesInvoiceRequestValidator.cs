using FluentValidation;
using Quay27.Application.Orders;

namespace Quay27.Application.Validators;

public sealed class CreateSalesInvoiceRequestValidator : AbstractValidator<CreateSalesInvoiceRequest>
{
    public CreateSalesInvoiceRequestValidator(CreateOrderItemRequestValidator lineValidator)
    {
        RuleFor(x => x.PaymentMethod)
            .Must(pm => !string.IsNullOrWhiteSpace(pm) &&
                        OrderReceivingAccountResolver.AllowedPaymentMethods.Contains(pm.Trim()))
            .WithMessage("Phương thức thanh toán không hợp lệ.");
        RuleFor(x => x.Note).MaximumLength(4000).When(x => x.Note != null);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(lineValidator);

        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClientDiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClientSubtotal).GreaterThanOrEqualTo(0);
    }
}
