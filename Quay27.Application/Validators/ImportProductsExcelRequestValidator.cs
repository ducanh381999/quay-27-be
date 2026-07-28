using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class ImportProductsExcelRequestValidator : AbstractValidator<ImportProductsExcelRequest>
{
    public ImportProductsExcelRequestValidator()
    {
        RuleFor(x => x.FileBytes).NotEmpty();
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("File must be an .xlsx workbook.");
        RuleFor(x => x.DuplicateCodeConflictAction)
            .Must(IsSupportedAction)
            .WithMessage("DuplicateCodeConflictAction must be 'error' or 'replace'.");
        RuleFor(x => x.DuplicateBarcodeConflictAction)
            .Must(IsSupportedAction)
            .WithMessage("DuplicateBarcodeConflictAction must be 'error' or 'replace'.");
    }

    private static bool IsSupportedAction(string? value) =>
        value is ProductImportConflictAction.Error or ProductImportConflictAction.Replace;
}
