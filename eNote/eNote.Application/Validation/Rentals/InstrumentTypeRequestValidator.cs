using eNote.Application.Common.Localization;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class InstrumentTypeRequestValidator : AbstractValidator<InstrumentTypeRequest>
{
    public InstrumentTypeRequestValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("Tip instrumenta je obavezan.").MaximumLength(100).WithMessage("Tip ne smije biti duži od 100 karaktera.");
        RuleFor(x => x.MonthlyFee)
            .GreaterThanOrEqualTo(0)
            .WithMessage(Messages.InstrumentTypeFeeMustBeNonNegative)
            .LessThanOrEqualTo(99_999_999.99m)   // fits InstrumentRental.Fee decimal(10,2) — see InstrumentRentalConfig.cs:34
            .WithMessage(Messages.InstrumentTypeFeeExceedsMaximum);
    }
}
