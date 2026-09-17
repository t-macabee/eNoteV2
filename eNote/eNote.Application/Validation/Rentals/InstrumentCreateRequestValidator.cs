using eNote.Application.Features.Rentals.Instruments;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class InstrumentCreateRequestValidator : AbstractValidator<InstrumentCreateRequest>
{
    public InstrumentCreateRequestValidator()
    {
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.InstrumentTypeId).GreaterThan(0);
    }
}
