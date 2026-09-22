using eNote.Application.Features.Rentals.InstrumentRentals;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

// 500 = RentalRequestSheet.noteMaxLength (UI/enote_mobile/lib/features/rentals/rental_request_sheet.dart:35)
public sealed class RentalStatusRequestValidator : AbstractValidator<RentalStatusRequest>
{
    public RentalStatusRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
