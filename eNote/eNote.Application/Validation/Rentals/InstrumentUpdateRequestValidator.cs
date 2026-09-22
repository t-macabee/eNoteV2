using eNote.Application.Features.Rentals.Instruments;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class InstrumentUpdateRequestValidator : AbstractValidator<InstrumentUpdateRequest>
{
    public InstrumentUpdateRequestValidator()
    {
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100).When(x => x.Model is not null);
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(100).When(x => x.Manufacturer is not null);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.InstrumentTypeId).GreaterThan(0).When(x => x.InstrumentTypeId.HasValue);
    }
}
