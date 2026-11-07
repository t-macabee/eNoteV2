using eNote.Application.Features.Rentals.Instruments;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class InstrumentRequestValidator : AbstractValidator<InstrumentRequest>
{
    public InstrumentRequestValidator()
    {
        RuleFor(x => x.Model).NotEmpty().MaximumLength(Instrument.ModelMaxLength);
        RuleFor(x => x.ManufacturerId).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(Instrument.DescriptionMaxLength);
        RuleFor(x => x.InstrumentTypeId).GreaterThan(0);
    }
}
