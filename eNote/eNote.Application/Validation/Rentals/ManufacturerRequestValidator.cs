using eNote.Application.Features.Rentals.ReferenceData.Manufacturers;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class ManufacturerRequestValidator : AbstractValidator<ManufacturerRequest>
{
    public ManufacturerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(Messages.ManufacturerNameRequired).MaximumLength(Manufacturer.NameMaxLength).WithMessage(Messages.ManufacturerNameTooLong);
    }
}
