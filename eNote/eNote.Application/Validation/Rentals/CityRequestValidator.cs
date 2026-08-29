using eNote.Application.Features.Rentals.ReferenceData.Cities;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class CityRequestValidator : AbstractValidator<CityRequest>
{
    public CityRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Naziv grada je obavezan.").MaximumLength(100).WithMessage("Naziv ne smije biti duži od 100 karaktera.");
    }
}
