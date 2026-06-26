using eNote.Application.Features.Rentals.Instruments;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class InstrumentCreateRequestValidator : AbstractValidator<InstrumentCreateRequest>
{
    public InstrumentCreateRequestValidator()
    {
        RuleFor(x => x.Model).NotEmpty();
        RuleFor(x => x.Manufacturer).NotEmpty();
        RuleFor(x => x.InstrumentTypeId).GreaterThan(0);
    }
}
