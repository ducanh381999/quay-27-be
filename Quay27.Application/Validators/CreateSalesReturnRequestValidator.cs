using FluentValidation;
using Quay27.Application.Orders;

namespace Quay27.Application.Validators;

public sealed class CreateSalesReturnRequestValidator : AbstractValidator<CreateSalesReturnRequest>
{
    public CreateSalesReturnRequestValidator(CreateOrderItemRequestValidator lineValidator)
    {
        RuleFor(x => x.Note).MaximumLength(4000).When(x => x.Note != null);

        RuleFor(x => x.ReturnItems).NotEmpty();
        RuleForEach(x => x.ReturnItems).SetValidator(lineValidator);

        RuleFor(x => x.ExchangeItems).NotEmpty()
            .When(x => x.ExchangeDelivery)
            .WithMessage("Cần thêm ít nhất một dòng khi chọn Giao hàng trong phần Mua hàng.");

        RuleForEach(x => x.ExchangeItems).SetValidator(lineValidator);

        RuleFor(x => x.ClientReturnDiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClientReturnFeeAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClientReturnSubtotal).GreaterThanOrEqualTo(0);

        RuleFor(x => x.ClientExchangeDiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClientExchangeSubtotal).GreaterThanOrEqualTo(0);
    }
}
