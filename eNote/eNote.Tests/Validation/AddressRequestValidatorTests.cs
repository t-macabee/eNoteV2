using eNote.Application.Features.ReferenceData.Addresses;
using eNote.Application.Validation.Rentals;

namespace eNote.Tests.Validation;

public sealed class AddressRequestValidatorTests
{
    private readonly AddressRequestValidator _validator = new();

    [Fact]
    public void Validate_AcceptsAValidRequest_AndTheLimits()
    {
        var result = _validator.Validate(new AddressRequest
        {
            CityId = 1,
            Street = new string('a', Address.StreetMaxLength),
            Number = new string('1', Address.NumberMaxLength)
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsACityThatIsNotChosen(int cityId)
    {
        var result = _validator.Validate(new AddressRequest { CityId = cityId, Street = "Bistrik", Number = "1" });

        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(AddressRequest.CityId) && e.ErrorMessage == "Grad je obavezan — odaberite iz liste.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsAnEmptyStreetAndNumber(string value)
    {
        var result = _validator.Validate(new AddressRequest { CityId = 1, Street = value, Number = value });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddressRequest.Street) && e.ErrorMessage == "Ulica je obavezna.");
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddressRequest.Number) && e.ErrorMessage == "Kućni broj je obavezan.");
    }

    [Fact]
    public void Validate_RejectsAStreetAndNumberOneCharacterOverTheLimit()
    {
        var result = _validator.Validate(new AddressRequest
        {
            CityId = 1,
            Street = new string('a', Address.StreetMaxLength + 1),
            Number = new string('1', Address.NumberMaxLength + 1)
        });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddressRequest.Street) && e.ErrorMessage == "Ulica ne smije biti duža od 100 karaktera.");
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddressRequest.Number) && e.ErrorMessage == "Broj ne smije biti duži od 20 karaktera.");
    }
}
