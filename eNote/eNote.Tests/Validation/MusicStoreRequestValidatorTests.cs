using eNote.Application.Common.Localization;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using eNote.Application.Validation.Rentals;

namespace eNote.Tests.Validation;

public sealed class MusicStoreRequestValidatorTests
{
    private readonly MusicStoreRequestValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("+38761123456")]
    [InlineData("+387 61 123 456")]
    [InlineData("033123456")]
    public void Validate_AcceptsValidPhoneNumber(string? phoneNumber)
    {
        var request = ValidRequest();
        request.PhoneNumber = phoneNumber;

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("+387-61-123-456")]
    [InlineData("1234567890123456")]
    [InlineData("+٣٨٧٦١١٢٣٤٥٦")]
    public void Validate_RejectsInvalidPhoneNumberFormat(string phoneNumber)
    {
        var request = ValidRequest();
        request.PhoneNumber = phoneNumber;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(MusicStoreRequest.PhoneNumber) && e.ErrorMessage == Messages.PhoneNumberFormat);
    }

    [Fact]
    public void Validate_RejectsPhoneNumberExceedingMaximumLength()
    {
        var request = ValidRequest();
        request.PhoneNumber = "+387" + new string(' ', 20) + "6112345";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(MusicStoreRequest.PhoneNumber) && e.ErrorMessage == Messages.PhoneNumberTooLong);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(MusicStoreRequest.PhoneNumber) && e.ErrorMessage == "Broj telefona ne smije biti duži od 30 karaktera.");
    }

    [Theory]
    [InlineData("09:00-17:00")]
    [InlineData("09:00 - 19:00")]
    [InlineData("00:00-23:59")]
    public void Validate_AcceptsValidBusinessHours(string businessHours)
    {
        var request = ValidRequest();
        request.BusinessHours = businessHours;

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("09-17")]
    [InlineData("25:00-26:00")]
    [InlineData("09:60-10:00")]
    public void Validate_RejectsInvalidBusinessHoursFormat(string businessHours)
    {
        var request = ValidRequest();
        request.BusinessHours = businessHours;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(MusicStoreRequest.BusinessHours) && e.ErrorMessage == Messages.BusinessHoursFormat);
    }

    private static MusicStoreRequest ValidRequest() => new()
    {
        StoreName = "Muzička Prodavnica",
        BusinessHours = "09:00-17:00",
        PhoneNumber = "+387 61 123 456"
    };
}
