using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class AddProductsByGroupsRequestValidator : AbstractValidator<AddProductsByGroupsRequest>
{
    public AddProductsByGroupsRequestValidator()
    {
        RuleFor(x => x.GroupIds).NotNull();
    }
}
