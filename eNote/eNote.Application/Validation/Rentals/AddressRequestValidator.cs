using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public AddressRequestValidator()
    {
        RuleFor(x => x.CityId).GreaterThan(0).WithMessage("Grad je obavezan — odaberite iz liste.");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Ulica je obavezna.").MaximumLength(100).WithMessage("Ulica ne smije biti duža od 100 karaktera.");
        RuleFor(x => x.Number).NotEmpty().WithMessage("Kućni broj je obavezan.").MaximumLength(20).WithMessage("Broj ne smije biti duži od 20 karaktera.");
    }
}
