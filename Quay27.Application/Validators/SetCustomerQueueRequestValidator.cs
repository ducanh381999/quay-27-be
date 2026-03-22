using FluentValidation;
using Quay27.Application.Customers;

namespace Quay27.Application.Validators;

public class SetCustomerQueueRequestValidator : AbstractValidator<SetCustomerQueueRequest>
{
    public SetCustomerQueueRequestValidator()
    {
        // bool has no extra rules
    }
}
