using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Validation.Rentals;

namespace eNote.Tests.Validation;

public sealed class InstrumentRequestValidatorTests
{
    private readonly InstrumentRequestValidator _validator = new();

    [Fact]
    public void Validate_AcceptsRequestWithAllFields()
    {
        var request = ValidRequest();

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_AcceptsNullOrBlankDescription(string? description)
    {
        var request = ValidRequest();
        request.Description = description;

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_AcceptsDescriptionOfMaximumLength()
    {
        var request = ValidRequest();
        request.Description = new string('a', Instrument.DescriptionMaxLength);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsModelThatIsNullOrBlank(string? model)
    {
        var request = ValidRequest();
        request.Model = model!;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentRequest.Model));
    }

    [Fact]
    public void Validate_RejectsModelLongerThanMaximum()
    {
        var request = ValidRequest();
        request.Model = new string('a', Instrument.ModelMaxLength + 1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentRequest.Model));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsNonPositiveManufacturerId(int manufacturerId)
    {
        var request = ValidRequest();
        request.ManufacturerId = manufacturerId;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentRequest.ManufacturerId));
    }

    [Fact]
    public void Validate_RejectsDescriptionLongerThanMaximum()
    {
        var request = ValidRequest();
        request.Description = new string('a', Instrument.DescriptionMaxLength + 1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentRequest.Description));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsNonPositiveInstrumentTypeId(int instrumentTypeId)
    {
        var request = ValidRequest();
        request.InstrumentTypeId = instrumentTypeId;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentRequest.InstrumentTypeId));
    }

    private static InstrumentRequest ValidRequest() => new()
    {
        Model = "Stratocaster",
        ManufacturerId = 1,
        Description = "Električna gitara",
        InstrumentTypeId = 1
    };
}
