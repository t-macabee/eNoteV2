using eNote.Application.Features.Rentals.InstrumentRentals;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class RentalStatusRequestValidator : AbstractValidator<RentalStatusRequest>
{
    public RentalStatusRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
