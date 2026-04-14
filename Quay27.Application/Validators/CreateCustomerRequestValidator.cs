using FluentValidation;
using Quay27.Application.Customers;

namespace Quay27.Application.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.NameAddress).MaximumLength(16_384);
        RuleFor(x => x.InvoiceCode).MaximumLength(64);
        RuleFor(x => x.CreateMachine).MaximumLength(128);
        RuleFor(x => x.DraftStaff).MaximumLength(128);
        RuleFor(x => x.InstallStaffCm).MaximumLength(128);
        RuleFor(x => x.GoodsSenderNote).MaximumLength(256);
        RuleFor(x => x.Status).MaximumLength(128);

    }
}
