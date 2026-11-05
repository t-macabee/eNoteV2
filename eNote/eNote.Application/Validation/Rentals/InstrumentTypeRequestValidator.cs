using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class InstrumentTypeRequestValidator : AbstractValidator<InstrumentTypeRequest>
{
    public InstrumentTypeRequestValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("Tip instrumenta je obavezan.").MaximumLength(InstrumentType.TypeMaxLength).WithMessage($"Tip ne smije biti duži od {InstrumentType.TypeMaxLength} karaktera.");
        RuleFor(x => x.MonthlyFee)
            .GreaterThanOrEqualTo(InstrumentType.MinMonthlyFee)
            .WithMessage(Messages.InstrumentTypeFeeBelowMinimum)
            .LessThanOrEqualTo(InstrumentType.MaxMonthlyFee)
            .WithMessage(Messages.InstrumentTypeFeeExceedsMaximum)
            .PrecisionScale(18, 2, false)
            .WithMessage(Messages.InstrumentTypeFeeTooManyDecimals);
    }
}
