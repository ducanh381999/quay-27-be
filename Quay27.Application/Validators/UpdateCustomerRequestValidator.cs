using FluentValidation;
using Quay27.Application.Customers;

namespace Quay27.Application.Validators;

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasAnyPatch)
            .WithMessage("At least one field must be provided.");

        When(x => x.NameAddress is not null, () => RuleFor(x => x.NameAddress!).NotEmpty());
        When(x => x.InvoiceCode is not null, () => RuleFor(x => x.InvoiceCode!).MaximumLength(64));
        When(x => x.CreateMachine is not null, () => RuleFor(x => x.CreateMachine!).MaximumLength(128));
        When(x => x.DraftStaff is not null, () => RuleFor(x => x.DraftStaff!).MaximumLength(128));
        When(x => x.InstallStaffCm is not null, () => RuleFor(x => x.InstallStaffCm!).MaximumLength(128));
        When(x => x.GoodsSenderNote is not null, () => RuleFor(x => x.GoodsSenderNote!).MaximumLength(256));
        When(x => x.Status is not null, () => RuleFor(x => x.Status!).MaximumLength(128));
        When(x => x.Quantity is not null, () => RuleFor(x => x.Quantity!).NotEmpty());
        When(x => x.TotalAmount is not null, () => RuleFor(x => x.TotalAmount!).MaximumLength(128));
    }

    private static bool HasAnyPatch(UpdateCustomerRequest r) =>
        r.SortOrder is not null
        || r.InvoiceCode is not null
        || r.BillCreatedAt is not null
        || r.NameAddress is not null
        || r.CreateMachine is not null
        || r.DraftStaff is not null
        || r.Quantity is not null
        || r.TotalAmount is not null
        || r.InstallStaffCm is not null
        || r.ManagerApproved is not null
        || r.Kio27Received is not null
        || r.Export27 is not null
        || r.FullSelfExport is not null
        || r.Notes is not null
        || r.GoodsSenderNote is not null
        || r.AdditionalNotes is not null
        || r.SheetDate is not null
        || r.Status is not null;
}
