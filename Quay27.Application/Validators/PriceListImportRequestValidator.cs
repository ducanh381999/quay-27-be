using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public sealed class PriceListImportRequestValidator : AbstractValidator<PriceListImportRequest>
{
    public PriceListImportRequestValidator()
    {
        RuleFor(x => x.FileBytes)
            .NotNull()
            .Must(x => x.Length > 0)
            .WithMessage("Import file is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(file => Path.GetExtension(file).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .xlsx file is supported.");
    }
}
