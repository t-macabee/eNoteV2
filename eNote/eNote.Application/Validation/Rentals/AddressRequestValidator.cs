using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public AddressRequestValidator()
    {
        RuleFor(x => x.CityId).GreaterThan(0).WithMessage("Grad je obavezan — odaberite iz liste.");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Ulica je obavezna.").MaximumLength(Address.StreetMaxLength).WithMessage($"Ulica ne smije biti duža od {Address.StreetMaxLength} karaktera.");
        RuleFor(x => x.Number).NotEmpty().WithMessage("Kućni broj je obavezan.").MaximumLength(Address.NumberMaxLength).WithMessage($"Broj ne smije biti duži od {Address.NumberMaxLength} karaktera.");
    }
}
