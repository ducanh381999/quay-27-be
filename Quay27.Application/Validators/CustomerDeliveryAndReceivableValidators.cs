using FluentValidation;
using Quay27.Application.CustomerProfiles;

namespace Quay27.Application.Validators;

public sealed class CreateCustomerDeliveryAddressRequestValidator : AbstractValidator<CreateCustomerDeliveryAddressRequest>
{
    public CreateCustomerDeliveryAddressRequestValidator()
    {
        RuleFor(x => x.AddressName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.AddressLine).NotEmpty();
        RuleFor(x => x.ProvinceCity).MaximumLength(128);
        RuleFor(x => x.Ward).MaximumLength(128);
    }
}

public sealed class PatchCustomerDeliveryAddressRequestValidator : AbstractValidator<PatchCustomerDeliveryAddressRequest>
{
    public PatchCustomerDeliveryAddressRequestValidator()
    {
        RuleFor(x => x.AddressName).MaximumLength(256).When(x => x.AddressName is not null);
        RuleFor(x => x.RecipientName).MaximumLength(256).When(x => x.RecipientName is not null);
        RuleFor(x => x.Phone).MaximumLength(32).When(x => x.Phone is not null);
        RuleFor(x => x.ProvinceCity).MaximumLength(128).When(x => x.ProvinceCity is not null);
        RuleFor(x => x.Ward).MaximumLength(128).When(x => x.Ward is not null);
    }
}

public sealed class RecordCustomerPaymentRequestValidator : AbstractValidator<RecordCustomerPaymentRequest>
{
    public RecordCustomerPaymentRequestValidator()
    {
        RuleFor(x => x.CollectorName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class RecordCustomerAdjustmentRequestValidator : AbstractValidator<RecordCustomerAdjustmentRequest>
{
    public RecordCustomerAdjustmentRequestValidator()
    {
        RuleFor(x => x.NewDebtAbsolute).GreaterThanOrEqualTo(0);
    }
}

public sealed class RecordCustomerPaymentDiscountRequestValidator : AbstractValidator<RecordCustomerPaymentDiscountRequest>
{
    public RecordCustomerPaymentDiscountRequestValidator()
    {
        RuleFor(x => x.PerformerName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.DiscountAmount).GreaterThan(0);
    }
}

public sealed class RecordCustomerQrPaymentRequestValidator : AbstractValidator<RecordCustomerQrPaymentRequest>
{
    public RecordCustomerQrPaymentRequestValidator()
    {
        RuleFor(x => x.ReceivingAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
