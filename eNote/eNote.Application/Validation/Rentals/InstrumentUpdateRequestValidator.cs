using eNote.Application.Features.Rentals.Instruments;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class InstrumentUpdateRequestValidator : AbstractValidator<InstrumentUpdateRequest>
{
    public InstrumentUpdateRequestValidator()
    {
        RuleFor(x => x.Model).NotEmpty().When(x => x.Model is not null);
        RuleFor(x => x.Manufacturer).NotEmpty().When(x => x.Manufacturer is not null);
        RuleFor(x => x.Description).NotEmpty().When(x => x.Description is not null);
        RuleFor(x => x.ImagePath).NotEmpty().When(x => x.ImagePath is not null);
        RuleFor(x => x.InstrumentTypeId).GreaterThan(0).When(x => x.InstrumentTypeId.HasValue);
    }
}
