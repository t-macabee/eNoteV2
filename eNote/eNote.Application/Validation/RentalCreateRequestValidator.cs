using eNote.Application.Features.InstrumentRentals;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class RentalCreateRequestValidator : AbstractValidator<RentalCreateRequest>
{
    public RentalCreateRequestValidator()
    {
        RuleFor(x => x.InstrumentId).GreaterThan(0);
    }
}
