using eNote.Application.Features.Rentals.Instruments;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class InstrumentCreateRequestValidator : AbstractValidator<InstrumentCreateRequest>
{
    public InstrumentCreateRequestValidator()
    {
        RuleFor(x => x.Model).NotEmpty().MaximumLength(Instrument.ModelMaxLength);
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(Instrument.ManufacturerMaxLength);
        RuleFor(x => x.Description).MaximumLength(Instrument.DescriptionMaxLength);
        RuleFor(x => x.InstrumentTypeId).GreaterThan(0);
    }
}
