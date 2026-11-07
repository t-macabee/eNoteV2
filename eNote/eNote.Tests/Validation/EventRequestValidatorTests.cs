using eNote.Application.Common.Localization;
using eNote.Application.Features.Communication.Events;
using eNote.Application.Validation.Communication;
using eNote.Domain.Entities.Communication;

namespace eNote.Tests.Validation;

public sealed class EventRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly EventRequestValidator _validator = new();

    [Fact]
    public void Validate_RejectsEndsBeforeStarts()
    {
        var result = _validator.Validate(new EventRequest
        {
            Title = "Bad Range",
            Description = "Invalid",
            StartsAt = Now.AddDays(2),
            EndsAt = Now.AddDays(1)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(EventRequest.EndsAt) && e.ErrorMessage == Messages.EventEndsBeforeStarts);
    }

    [Fact]
    public void Validate_AcceptsEndsAfterStarts()
    {
        var result = _validator.Validate(new EventRequest
        {
            Title = "Valid Range",
            Description = "Valid",
            StartsAt = Now.AddDays(2),
            EndsAt = Now.AddDays(3)
        });

        Assert.True(result.IsValid);
    }

    private static EventRequest Valid() => new()
    {
        Title = "Koncert",
        Description = "Opis",
        StartsAt = Now.AddDays(1)
    };

    [Fact]
    public void Validate_AcceptsTheTitleAndDescriptionLimits()
    {
        var request = Valid();
        request.Title = new string('a', Event.TitleMaxLength);
        request.Description = new string('b', Event.DescriptionMaxLength);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsAnEmptyTitleAndDescription(string value)
    {
        var request = Valid();
        request.Title = value;
        request.Description = value;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EventRequest.Title) && e.ErrorMessage == "Naslov je obavezan.");
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EventRequest.Description) && e.ErrorMessage == "Opis je obavezan.");
    }

    [Fact]
    public void Validate_RejectsATitleAndDescriptionOneCharacterOverTheLimit()
    {
        var request = Valid();
        request.Title = new string('a', Event.TitleMaxLength + 1);
        request.Description = new string('b', Event.DescriptionMaxLength + 1);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EventRequest.Title) && e.ErrorMessage == "Naslov ne smije biti duži od 150 karaktera.");
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EventRequest.Description) && e.ErrorMessage == "Opis ne smije biti duži od 4000 karaktera.");
    }

    [Fact]
    public void Validate_RejectsAMissingStart()
    {
        var request = Valid();
        request.StartsAt = default;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EventRequest.StartsAt) && e.ErrorMessage == "Vrijeme početka je obavezno.");
    }

    [Fact]
    public void Validate_RejectsAnEndEqualToTheStart()
    {
        var request = Valid();
        request.EndsAt = request.StartsAt;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(EventRequest.EndsAt) && e.ErrorMessage == Messages.EventEndsBeforeStarts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsAnAddressIdBelowOne(int addressId)
    {
        var request = Valid();
        request.AddressId = addressId;

        Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == nameof(EventRequest.AddressId));
    }

    [Fact]
    public void Validate_AcceptsNoAddress()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }
}
