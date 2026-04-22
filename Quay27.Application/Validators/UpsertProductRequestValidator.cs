using FluentValidation;
using Quay27.Application.Products;

namespace Quay27.Application.Validators;

public class UpsertProductRequestValidator : AbstractValidator<UpsertProductRequest>
{
    public UpsertProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).MaximumLength(64);
        RuleFor(x => x.ItemType).NotEmpty().Must(x => x is "goods" or "service" or "combo");
        RuleFor(x => x.Barcode).MaximumLength(64);
        RuleFor(x => x.GroupName).MaximumLength(128);
        RuleFor(x => x.Brand).MaximumLength(128);
        RuleFor(x => x.Location).MaximumLength(128);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x)
            .Must(x => x.MinStock <= x.MaxStock)
            .WithMessage("MinStock cannot be greater than MaxStock.");
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.DescriptionRichText).MaximumLength(4000);
        RuleFor(x => x.InvoiceNoteTemplate).MaximumLength(4000);
        RuleFor(x => x.WeightUnit).NotEmpty().Must(x => x is "g" or "kg");
        RuleFor(x => x.WeightValue).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.UploadedImageAssets).ChildRules(asset =>
        {
            asset.RuleFor(x => x.AssetId).NotEmpty().MaximumLength(128);
            asset.RuleFor(x => x.Url).NotEmpty().MaximumLength(1024);
        });
    }
}
