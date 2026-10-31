using eNote.Application.Features.ReferenceData.Cities;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class CityRequestValidator : AbstractValidator<CityRequest>
{
    public CityRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Naziv grada je obavezan.").MaximumLength(City.NameMaxLength).WithMessage($"Naziv ne smije biti duži od {City.NameMaxLength} karaktera.");
    }
}
