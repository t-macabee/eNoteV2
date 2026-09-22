using eNote.Application.Features.Rentals.InstrumentRentals;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class RentalCreateRequestValidator : AbstractValidator<RentalCreateRequest>
{
    public RentalCreateRequestValidator()
    {
        RuleFor(x => x.InstrumentId).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
