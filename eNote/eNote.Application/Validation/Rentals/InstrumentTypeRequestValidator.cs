using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class InstrumentTypeRequestValidator : AbstractValidator<InstrumentTypeRequest>
{
    public InstrumentTypeRequestValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("Tip instrumenta je obavezan.").MaximumLength(100).WithMessage("Tip ne smije biti duži od 100 karaktera.");
        RuleFor(x => x.MonthlyFee).GreaterThanOrEqualTo(0).WithMessage("Mjesečna naknada mora biti nenegativan decimalni broj.");
    }
}
